using TradingEngine.Domain.Enums;

namespace TradingEngine.MatchingEngine.Models.Notifications;

public record TradeNotification(
    Guid TradeId,
    string Symbol,
    decimal Price,
    decimal Quantity,
    OrderSide AggressorSide,
    long ExecutedAt);

public record OrderBookNotification(
    string Symbol,
    List<OrderBookEntry> StateChanges);

public record OrderBookEntry(
    decimal Price,
    decimal Quantity,
    bool IsBuy);

public record OrderBookStateChangeDto(
    long Price,
    long Quantity,
    bool IsBuy);

public record OrderStatusNotification(
    Guid OrderId,
    OrderStatus Status,
    decimal FilledQuantity,
    decimal RemainingQuantity);