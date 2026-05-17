using TradingEngine.Domain.Common;
using TradingEngine.Domain.ValueObjects;

namespace TradingEngine.Domain.Events.Orders;

public class OrderFilledEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid UserId { get; }
    public Guid SymbolId { get; }
    public Quantity Quantity { get; }

    public OrderFilledEvent(
        Guid orderId,
        Guid userId,
        Guid symbolId,
        Quantity quantity)
    {
        AggregateId = orderId;

        OrderId = orderId;
        UserId = userId;
        SymbolId = symbolId;
        Quantity = quantity;
    }
}