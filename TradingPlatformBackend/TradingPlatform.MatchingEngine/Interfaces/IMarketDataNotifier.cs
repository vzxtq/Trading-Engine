using TradingEngine.MatchingEngine.Models.Notifications;

namespace TradingEngine.MatchingEngine.Interfaces;

public interface IMarketDataNotifier
{
    Task NotifyTradeExecutedAsync(TradeNotification notification, CancellationToken ct);
    Task NotifyOrderBookUpdatedAsync(OrderBookNotification notification, CancellationToken ct);
    Task NotifyOrderStatusChangedAsync(string userId, OrderStatusNotification notification, CancellationToken ct);
}
