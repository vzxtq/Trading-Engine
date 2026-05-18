using TradingEngine.Domain.Enums;
using TradingEngine.Domain.ValueObjects;

namespace TradingEngine.MatchingEngine.Models;

/// <summary>
/// Engine-local order representation. Pure data + minimal mutation helpers.
/// </summary>
public class EngineOrder
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public Symbol Symbol { get; }
    public Guid SymbolId { get; }
    public long Price { get; }
    public long OriginalQuantity { get; }
    public long FilledQuantity { get; private set; }
    public long RemainingQuantity { get; private set; }
    public OrderSide Side { get; }
    public long CreatedAt { get; }

    public bool IsFullyMatched => RemainingQuantity == 0;

    public EngineOrder(
        Guid id,
        Guid userId,
        Symbol symbol,
        Guid symbolId,
        long price,
        long quantity,
        OrderSide side,
        long createdAt)
    {
        if (price <= 0) throw new ArgumentOutOfRangeException(nameof(price));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));

        Id = id;
        UserId = userId;
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        SymbolId = symbolId;
        Price = price;
        OriginalQuantity = quantity;
        FilledQuantity = 0;
        RemainingQuantity = quantity;
        Side = side;
        CreatedAt = createdAt;
    }

    public void Fill(long quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (quantity > RemainingQuantity)
            throw new InvalidOperationException("Cannot fill by more than remaining quantity");

        FilledQuantity += quantity;
        RemainingQuantity -= quantity;
    }

    public void Reduce(long quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (quantity > RemainingQuantity)
            throw new InvalidOperationException("Cannot reduce by more than remaining quantity");

        RemainingQuantity -= quantity;
    }
}
