using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingEngine.Domain.Enums;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.ValueObjects;
using TradingEngine.Infrastructure.Persistence;
using TradingEngine.MatchingEngine.Interfaces;
using TradingEngine.MatchingEngine.Models;

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
                .ToList();

            var orders = await _dbContext.Orders
                .Where(o => orderIds.Contains(o.Id))
                .ToDictionaryAsync(o => o.Id, cancellationToken);

            var userIds = orders.Values.Select(o => o.UserId)
                .Concat(accepted.Trades.SelectMany(t => new[] { t.BuyerId, t.SellerId }))
                .Distinct()
                .ToList();

            var accounts = await _dbContext.UserAccounts
                .Where(a => userIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, cancellationToken);

            var symbols = orders.Values.Select(o => o.Symbol.Name).Distinct().ToList();
            var positions = await _dbContext.Positions
                .Where(p => userIds.Contains(p.UserId) && symbols.Contains(p.SymbolValue.Value))
                .ToListAsync(cancellationToken);

            PositionDomain? FindPosition(Guid userId, Symbol symbol) =>
                positions.FirstOrDefault(p => p.UserId == userId && p.SymbolValue == symbol);

            foreach (var trade in accepted.Trades)
            {
                var buyOrder = orders[trade.BuyOrderId];

                var price = new Price(trade.Price);
                var qty = new Quantity(trade.Quantity);
                var notional = price.Value * qty.Value;
                var currency = accounts[trade.BuyerId].Balance.Currency;
                var money = new Money(notional, currency);

                accounts[trade.BuyerId].CommitReservedFunds(money);
                accounts[trade.SellerId].Deposit(money);

                var buyPos = FindPosition(trade.BuyerId, new Symbol(buyOrder.Symbol.Name));
                if (buyPos is null)
                {
                    buyPos = PositionDomain.Create(trade.BuyerId, new Symbol(buyOrder.Symbol.Name), qty, price.Value)
                        ?? throw new UnreachableException("PositionDomain.Create returned null unexpectedly.");
                    _dbContext.Positions.Add(buyPos);
                    positions.Add(buyPos);
                }
                else
                {
                    buyPos.Add(qty, price.Value);
                }

                var sellPos = FindPosition(trade.SellerId, new Symbol(buyOrder.Symbol.Name))
                    ?? throw new InvalidOperationException($"Seller position not found for symbol '{buyOrder.Symbol.Name}'.");

                sellPos.CommitReserved(qty);

                var executedAt = DateTimeOffset.FromUnixTimeMilliseconds(trade.ExecutedAt).UtcDateTime;
                var tradeDomain = TradeDomain.Create(
                    trade.TradeId,
                    trade.BuyOrderId,
                    trade.SellOrderId,
                    trade.BuyerId,
                    trade.SellerId,
                    new Symbol(buyOrder.Symbol.Name),
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

                order.ApplyStateChange(stateChange.FilledQuantity, stateChange.Status);

                if (order.Side == OrderSide.Buy
                    && stateChange.Status == OrderStatus.Cancelled
                    && stateChange.RemainingQuantity > 0)
                {
                    var release = new Money(
                        order.Price.Value * stateChange.RemainingQuantity,
                        accounts[order.UserId].Balance.Currency);

                    accounts[order.UserId].ReleaseReservedFunds(release);
                }
                else if (order.Side == OrderSide.Sell
                    && stateChange.Status == OrderStatus.Cancelled
                    && stateChange.RemainingQuantity > 0)
                {
                    FindPosition(order.UserId, new Symbol(order.Symbol.Name))
                        ?.ReleaseReserved(new Quantity(stateChange.RemainingQuantity));
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
        }
    }
}