using System.Diagnostics;
using TradingEngine.MatchingEngine.Interfaces;
using TradingEngine.MatchingEngine.Models;
using TradingEngine.MatchingEngine.Models.Notifications;

namespace TradingEngine.MatchingEngine.Handlers;

public class MarketDataExecutionResultHandler : IExecutionResultHandler
{
    private readonly IMarketDataNotifier _notifier;

    public MarketDataExecutionResultHandler(IMarketDataNotifier notifier)
    {
        _notifier = notifier;
    }

    public async Task HandleAsync(ExecutionResult result, CancellationToken cancellationToken)
    {
        Task task = result switch
        {
            ExecutionResult.Accepted accepted => HandleAcceptedAsync(accepted, cancellationToken),
            ExecutionResult.Rejected => Task.CompletedTask,
            _ => throw new UnreachableException()
        };

        await task;
    }

    private async Task HandleAcceptedAsync(ExecutionResult.Accepted accepted, CancellationToken cancellationToken)
    {
        try
        {
            if (accepted.Symbol == null) return;

            if (accepted.Trades != null)
            {
                foreach (var trade in accepted.Trades)
                {
                    var notification = new TradeNotification(
                        accepted.Symbol.Value,
                        trade.Price,
                        trade.Quantity,
                        accepted.EngineTimestamp);
                    
                    await _notifier.NotifyTradeExecutedAsync(notification, cancellationToken);
                }
            }

            if (accepted.StateChanges != null)
            {
                foreach (var stateChange in accepted.StateChanges)
                {
                    var notification = new OrderStatusNotification(
                        stateChange.OrderId,
                        stateChange.Status,
                        stateChange.FilledQuantity,
                        stateChange.RemainingQuantity);

                    await _notifier.NotifyOrderStatusChangedAsync(stateChange.UserId.ToString(), notification, cancellationToken);
                }
            }
        }
        catch (Exception)
        {
            // Swallow to avoid impacting the engine pipeline; log in real implementation.
        }
    }
}
