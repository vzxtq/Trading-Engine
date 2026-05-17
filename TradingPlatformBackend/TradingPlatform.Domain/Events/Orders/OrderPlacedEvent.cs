using TradingEngine.Domain.Enums;
using TradingEngine.Domain.ValueObjects;

namespace TradingEngine.Domain.Events.Orders;

/// <summary>
/// Raised when an order has been placed in the system.
/// </summary>
public class OrderPlacedEvent : DomainEvent
{
    public Guid UserId { get; }
    public Guid SymbolId { get; }
    public Price Price { get; }
    public Quantity Quantity { get; }
    public OrderSide Side { get; }

    public OrderPlacedEvent(Guid orderId, Guid userId, Guid symbolId, Price price, Quantity quantity, OrderSide side)
    {
        AggregateId = orderId;
        UserId = userId;
        SymbolId = symbolId;
        Price = price;
        Quantity = quantity;
        Side = side;
    }
    }