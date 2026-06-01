using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingEngine.Domain.Enums;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.ValueObjects;
using TradingEngine.Infrastructure.Persistence;
using TradingEngine.MatchingEngine.Interfaces;
using TradingEngine.MatchingEngine.Models;
using TradingEngine.MatchingEngine.Scaling;

namespace TradingEngine.Infrastructure.Handlers;

/// <summary>
/// Persists trades and updates balances/positions based on execution results from the matching engine.
/// Lives in Infrastructure to access DbContext without creating project reference cycles.
/// </summary>
public sealed class PersistenceExecutionResultHandler : IExecutionResultHandler
{
    private readonly TradingDbContext _dbContext;
    private readonly ILogger<PersistenceExecutionResultHandler> _logger;

    public PersistenceExecutionResultHandler(
        TradingDbContext dbContext,
        ILogger<PersistenceExecutionResultHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task HandleAsync(ExecutionResult result, CancellationToken cancellationToken)
    {
        if (result is not ExecutionResult.Accepted accepted)
            return;

        await PersistAcceptedAsync(accepted, cancellationToken);
    }

    private async Task PersistAcceptedAsync(ExecutionResult.Accepted accepted, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var orderIds = accepted.Trades
                .SelectMany(t => new[] { t.BuyOrderId, t.SellOrderId })
                .Concat(accepted.StateChanges.Select(sc => sc.OrderId))
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            if (orderIds.Any())
            {
                var idList = string.Join(",", orderIds.Select(id => $"'{id}'"));
                await _dbContext.Database.ExecuteSqlRawAsync($"SELECT 1 FROM Orders WITH (UPDLOCK) WHERE Id IN ({idList})", cancellationToken);
            }

            var orders = await _dbContext.Orders
                .Include(o => o.Symbol)
                .Where(o => orderIds.Contains(o.Id))
                .ToDictionaryAsync(o => o.Id, cancellationToken);

            var userIds = orders.Values.Select(o => o.UserId)
                .Concat(accepted.Trades.SelectMany(t => new[] { t.BuyerId, t.SellerId }))
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            if (userIds.Any())
            {
                var idList = string.Join(",", userIds.Select(id => $"'{id}'"));
                await _dbContext.Database.ExecuteSqlRawAsync($"SELECT 1 FROM UserAccounts WITH (UPDLOCK) WHERE Id IN ({idList})", cancellationToken);
            }

            var accounts = await _dbContext.UserAccounts
                .Where(a => userIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, cancellationToken);

            var symbolsInBatch = orders.Values.Select(o => o.Symbol.Name).Distinct().ToHashSet();
            
            if (userIds.Any())
            {
                var idList = string.Join(",", userIds.Select(id => $"'{id}'"));
                await _dbContext.Database.ExecuteSqlRawAsync($"SELECT 1 FROM Positions WITH (UPDLOCK) WHERE UserId IN ({idList})", cancellationToken);
            }

            var allPositions = await _dbContext.Positions
                .Where(p => userIds.Contains(p.UserId))
                .ToListAsync(cancellationToken);

            var positionCache = allPositions
                .Where(p => symbolsInBatch.Contains(p.SymbolValue.Value))
                .ToDictionary(p => (p.UserId, p.SymbolValue.Value), p => p);

            PositionDomain? FindPosition(Guid userId, string symbol) =>
                positionCache.TryGetValue((userId, symbol), out var p) ? p : null;

            foreach (var trade in accepted.Trades)
            {
                var buyOrder = orders[trade.BuyOrderId];
                var symbolName = buyOrder.Symbol.Name;

                decimal priceValue = trade.Price.ToDomainPrice();
                decimal quantityValue = trade.Quantity.ToDomainQuantity();

                var price = new Price(priceValue);
                var qty = new Quantity(quantityValue);
                var notional = priceValue * quantityValue;
                var currency = accounts[trade.BuyerId].Balance.Currency;
                var money = new Money(notional, currency);

                accounts[trade.BuyerId].CommitReservedFunds(money);
                accounts[trade.SellerId].Deposit(money);

                var buyPos = FindPosition(trade.BuyerId, symbolName);
                if (buyPos is null)
                {
                    buyPos = PositionDomain.Create(trade.BuyerId, new Symbol(symbolName), qty, priceValue)
                        ?? throw new UnreachableException("PositionDomain.Create returned null unexpectedly.");
                    _dbContext.Positions.Add(buyPos);
                    positionCache[(trade.BuyerId, symbolName)] = buyPos;
                }
                else
                {
                    buyPos.Add(qty, priceValue);
                }

                var sellPos = FindPosition(trade.SellerId, symbolName);
                if (sellPos is null)
                {
                    _logger.LogWarning("Seller position not found for user {UserId} and symbol {Symbol}. Skipping position update.", trade.SellerId, symbolName);
                }
                else
                {
                    sellPos.CommitReserved(qty);
                }

                var executedAt = DateTimeOffset.FromUnixTimeMilliseconds(trade.ExecutedAt).UtcDateTime;
                var tradeDomain = TradeDomain.Create(
                    trade.TradeId,
                    trade.BuyOrderId,
                    trade.SellOrderId,
                    trade.BuyerId,
                    trade.SellerId,
                    trade.SymbolId,
                    price,
                    qty,
                    executedAt);

                _dbContext.Trades.Add(tradeDomain);
            }

            foreach (var stateChange in accepted.StateChanges)
            {
                if (!orders.TryGetValue(stateChange.OrderId, out var order))
                    continue;

                _logger.LogInformation(
                    "Processing state change for order {OrderId}: {Status}, Filled: {Filled}",
                    stateChange.OrderId, stateChange.Status, stateChange.FilledQuantity);

                order.ApplyStateChange(stateChange.FilledQuantity.ToDomainQuantity(), stateChange.Status);

                if (order.Side == OrderSide.Buy
                    && (stateChange.Status == OrderStatus.Cancelled || stateChange.Status == OrderStatus.PartiallyFilledCancelled)
                    && stateChange.RemainingQuantity > 0)
                {
                    var spentInPreviousBatches = await _dbContext.Trades
                        .Where(t => t.BuyOrderId == order.Id)
                        .SumAsync(t => t.Price.Value * t.Quantity.Value, cancellationToken);

                    var spentInThisBatch = accepted.Trades
                        .Where(t => t.BuyOrderId == order.Id)
                        .Sum(t => t.Price.ToDomainPrice() * t.Quantity.ToDomainQuantity());

                    decimal releaseAmount = order.ReservedAmount - (spentInPreviousBatches + spentInThisBatch);

                    var release = new Money(
                        Math.Max(0, releaseAmount),
                        accounts[order.UserId].Balance.Currency);

                    accounts[order.UserId].ReleaseReservedFunds(release);
                }
                else if (order.Side == OrderSide.Sell
                    && (stateChange.Status == OrderStatus.Cancelled || stateChange.Status == OrderStatus.PartiallyFilledCancelled)
                    && stateChange.RemainingQuantity > 0)
                {
                    FindPosition(order.UserId, order.Symbol.Name)
                        ?.ReleaseReserved(new Quantity(stateChange.RemainingQuantity.ToDomainQuantity()));
                }

                _dbContext.Orders.Update(order);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist execution result");
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}