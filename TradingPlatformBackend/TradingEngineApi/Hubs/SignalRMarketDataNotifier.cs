using Microsoft.AspNetCore.SignalR;
using TradingEngine.MatchingEngine.Interfaces;
using TradingEngine.MatchingEngine.Models.Notifications;

namespace TradingEngine.Api.Hubs
{
    public class SignalRMarketDataNotifier : IMarketDataNotifier
    {
        private readonly IHubContext<MarketDataHub> _marketDataHub;
        private readonly IHubContext<OrderHub> _orderHub;

        public SignalRMarketDataNotifier(
            IHubContext<MarketDataHub> marketDataHub,
            IHubContext<OrderHub> orderHub)
        {
            _marketDataHub = marketDataHub;
            _orderHub = orderHub;
        }

        public async Task NotifyTradeExecutedAsync(TradeNotification notification, CancellationToken ct)
        {
            await _marketDataHub.Clients.Group(notification.Symbol)
                .SendAsync("TradeExecuted", notification, ct);
        }

        public async Task NotifyOrderBookUpdatedAsync(OrderBookNotification notification, CancellationToken ct)
        {
            await _marketDataHub.Clients.Group(notification.Symbol)
                .SendAsync("OrderBookUpdated", notification, ct);
        }

        public async Task NotifyOrderStatusChangedAsync(string userId, OrderStatusNotification notification, CancellationToken ct)
        {
            await _orderHub.Clients.Group($"User_{userId}")
                .SendAsync("OrderStatusChanged", notification, ct);
        }
    }
}
