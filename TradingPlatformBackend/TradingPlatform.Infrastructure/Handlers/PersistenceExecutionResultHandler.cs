using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradingEngine.Application.Interfaces;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.Enums;
using TradingEngine.Domain.ValueObjects;
using TradingEngine.Infrastructure.Persistence;
using TradingEngine.Infrastructure.Persistence.Outbox;
using TradingEngine.MatchingEngine.Models;
using TradingEngine.MatchingEngine.Scaling;

namespace TradingEngine.Infrastructure.Handlers;

public sealed class PersistenceExecutionResultHandler
{
    private const int MaxAttempts = 4;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PersistenceExecutionResultHandler> _logger;

    public PersistenceExecutionResultHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<PersistenceExecutionResultHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ProcessAsync(
        Guid resultOutboxId,
        ExecutionResult result,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await ProcessAttemptAsync(resultOutboxId, result, cancellationToken);
                return;
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < MaxAttempts)
            {
                await DelayBeforeRetryAsync(result, attempt, ex, cancellationToken);
            }
            catch (DbUpdateException ex) when (attempt < MaxAttempts)
            {
                await DelayBeforeRetryAsync(result, attempt, ex, cancellationToken);
            }
        }
    }

    private async Task ProcessAttemptAsync(
        Guid resultOutboxId,
        ExecutionResult result,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var outboxEntry = await dbContext.ExecutionResultOutbox
                .FirstOrDefaultAsync(x => x.Id == resultOutboxId, ct)
                ?? throw new InvalidOperationException(
                    $"Execution result outbox entry {resultOutboxId} was not found.");

            if (outboxEntry.Status == ExecutionResultOutboxStatus.Processed)
                return true;

            var alreadyProcessed = await dbContext.ProcessedExecutionReceipts
                .AnyAsync(
                    x => x.SymbolId == result.SymbolId
                         && x.SequenceId == result.SequenceId,
                    ct);

            if (!alreadyProcessed)
            {
                if (result is ExecutionResult.Accepted accepted)
                    await ApplyAcceptedAsync(dbContext, accepted, ct);

                await dbContext.ProcessedExecutionReceipts.AddAsync(
                    ProcessedExecutionReceipt.Create(
                        result.SymbolId,
                        result.SequenceId),
                    ct);
            }

            var commandEntry = await dbContext.OrderCommandOutbox
                .FirstOrDefaultAsync(
                    x => x.Id == outboxEntry.CommandOutboxId,
                    ct)
                ?? throw new InvalidOperationException(
                    $"Order command outbox entry {outboxEntry.CommandOutboxId} was not found.");

            commandEntry.MarkSettled();
            outboxEntry.MarkProcessed();

            await unitOfWork.CommitAsync(ct);

            return true;
        }, cancellationToken);
    }

    private async Task ApplyAcceptedAsync(
        TradingDbContext dbContext,
        ExecutionResult.Accepted accepted,
        CancellationToken cancellationToken)
    {
        var orderIds = accepted.Trades
            .SelectMany(t => new[] { t.BuyOrderId, t.SellOrderId })
            .Concat(accepted.StateChanges.Select(sc => sc.OrderId))
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        var orders = await dbContext.Orders
            .Include(o => o.Symbol)
            .Where(o => orderIds.Contains(o.Id))
            .OrderBy(o => o.Id)
            .ToDictionaryAsync(o => o.Id, cancellationToken);

        var userIds = orders.Values
            .Select(o => o.UserId)
            .Concat(accepted.Trades.SelectMany(t => new[] { t.BuyerId, t.SellerId }))
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        var accounts = await dbContext.UserAccounts
            .Where(a => userIds.Contains(a.Id))
            .OrderBy(a => a.Id)
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        var symbolsInBatch = orders.Values
            .Select(o => o.Symbol.Name)
            .Distinct()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allPositions = await dbContext.Positions
            .Where(p => userIds.Contains(p.UserId))
            .OrderBy(p => p.UserId)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);

        var positionCache = allPositions
            .Where(p => symbolsInBatch.Contains(p.SymbolValue.Value))
            .ToDictionary(
                p => (p.UserId, p.SymbolValue.Value),
                p => p);

        PositionDomain? FindPosition(Guid userId, string symbol)
        {
            return positionCache.TryGetValue((userId, symbol), out var position)
                ? position
                : null;
        }

        foreach (var trade in accepted.Trades)
        {
            var buyOrder = orders[trade.BuyOrderId];
            var symbolName = buyOrder.Symbol.Name;

            var priceValue = trade.Price.ToDomainPrice();
            var quantityValue = trade.Quantity.ToDomainQuantity();
            var price = new Price(priceValue);
            var quantity = new Quantity(quantityValue);
            var notional = priceValue * quantityValue;
            var currency = accounts[trade.BuyerId].Balance.Currency;
            var money = new Money(notional, currency);

            accounts[trade.BuyerId].CommitReservedFunds(money);
            accounts[trade.SellerId].Deposit(money);

            var buyPosition = FindPosition(trade.BuyerId, symbolName);
            if (buyPosition is null)
            {
                buyPosition = PositionDomain.Create(
                                  trade.BuyerId,
                                  new Symbol(symbolName),
                                  quantity,
                                  priceValue)
                              ?? throw new UnreachableException(
                                  "PositionDomain.Create returned null unexpectedly.");

                await dbContext.Positions.AddAsync(buyPosition, cancellationToken);
                positionCache[(trade.BuyerId, symbolName)] = buyPosition;
            }
            else
            {
                buyPosition.Add(quantity, priceValue);
            }

            var sellPosition = FindPosition(trade.SellerId, symbolName);
            if (sellPosition is null)
            {
                _logger.LogWarning(
                    "Seller position not found for user {UserId} and symbol {Symbol}",
                    trade.SellerId,
                    symbolName);
            }
            else
            {
                sellPosition.CommitReserved(quantity);
            }

            var executedAt = DateTimeOffset
                .FromUnixTimeMilliseconds(trade.ExecutedAt)
                .UtcDateTime;

            var tradeDomain = TradeDomain.Create(
                trade.TradeId,
                trade.BuyOrderId,
                trade.SellOrderId,
                trade.BuyerId,
                trade.SellerId,
                trade.SymbolId,
                price,
                quantity,
                executedAt);

            await dbContext.Trades.AddAsync(tradeDomain, cancellationToken);
        }

        foreach (var stateChange in accepted.StateChanges)
        {
            if (!orders.TryGetValue(stateChange.OrderId, out var order))
                continue;

            var statusBeforeChange = order.Status;
            order.ApplyStateChange(
                stateChange.FilledQuantity.ToDomainQuantity(),
                stateChange.Status);

            var isFirstCancellation =
                statusBeforeChange != OrderStatus.Cancelled
                && statusBeforeChange != OrderStatus.PartiallyFilledCancelled;

            var isCancellation =
                stateChange.Status == OrderStatus.Cancelled
                || stateChange.Status == OrderStatus.PartiallyFilledCancelled;

            if (isFirstCancellation
                && isCancellation
                && stateChange.RemainingQuantity > 0
                && order.Side == OrderSide.Buy)
            {
                var previousTrades = await dbContext.Trades
                    .Where(t => t.BuyOrderId == order.Id)
                    .Select(t => new
                    {
                        PriceValue = t.Price.Value,
                        QuantityValue = t.Quantity.Value
                    })
                    .ToListAsync(cancellationToken);

                var spentInPreviousBatches = previousTrades
                    .Sum(t => t.PriceValue * t.QuantityValue);

                var spentInThisBatch = accepted.Trades
                    .Where(t => t.BuyOrderId == order.Id)
                    .Sum(t => t.Price.ToDomainPrice() * t.Quantity.ToDomainQuantity());

                var releaseAmount = Math.Max(
                    0,
                    order.ReservedAmount - spentInPreviousBatches - spentInThisBatch);

                if (releaseAmount > 0)
                {
                    accounts[order.UserId].ReleaseReservedFunds(
                        new Money(
                            releaseAmount,
                            accounts[order.UserId].Balance.Currency));
                }
            }
            else if (isFirstCancellation
                     && isCancellation
                     && stateChange.RemainingQuantity > 0
                     && order.Side == OrderSide.Sell)
            {
                FindPosition(order.UserId, order.Symbol.Name)
                    ?.ReleaseReserved(
                        new Quantity(
                            stateChange.RemainingQuantity.ToDomainQuantity()));
            }
        }
    }

    private async Task DelayBeforeRetryAsync(
        ExecutionResult result,
        int attempt,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var delayMilliseconds = attempt * 25 + Random.Shared.Next(5, 25);
        _logger.LogWarning(
            exception,
            "Concurrency conflict settling symbol {SymbolId}, sequence {SequenceId}. Retrying attempt {Attempt}",
            result.SymbolId,
            result.SequenceId,
            attempt + 1);

        await Task.Delay(delayMilliseconds, cancellationToken);
    }
}
