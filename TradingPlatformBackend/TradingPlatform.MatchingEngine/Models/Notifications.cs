using TradingEngine.Domain.Enums;

namespace TradingEngine.MatchingEngine.Models.Notifications;

public record TradeNotification(
    string Symbol,
    decimal Price,
    decimal Quantity,
    long ExecutedAt);

public record OrderBookNotification(
    string Symbol,
    List<OrderBookStateChangeDto> StateChanges);

public record OrderBookStateChangeDto(
    decimal Price,
    decimal Quantity,
    bool IsBuy);

public record OrderStatusNotification(
    Guid OrderId,
    OrderStatus Status,
    decimal FilledQuantity,
    decimal RemainingQuantity);
