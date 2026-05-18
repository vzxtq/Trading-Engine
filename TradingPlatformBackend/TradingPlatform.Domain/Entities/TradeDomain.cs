using TradingEngine.Domain.Common;
using TradingEngine.Domain.ValueObjects;

namespace TradingEngine.Domain.Entities;

/// <summary>
/// Represents an executed trade (match between a buy and sell order).
/// </summary>
public class TradeDomain : BaseEntity
{
    public Guid BuyOrderId { get; private set; }
    public Guid SellOrderId { get; private set; }
    public Guid BuyerId { get; private set; }
    public Guid SellerId { get; private set; }
    public Guid SymbolId { get; private set; }
    public virtual SymbolDomain Symbol { get; private set; } = null!;
    public Price Price { get; private set; } = null!;
    public Quantity Quantity { get; private set; } = null!;
    public DateTime ExecutedAt { get; private set; }

    private TradeDomain()
    { }

    /// <summary>
    /// Creates a new executed trade.
    /// </summary>
    public static TradeDomain Create(
        Guid buyOrderId,
        Guid sellOrderId,
        Guid buyerId,
        Guid sellerId,
        Guid symbolId,
        Price price,
        Quantity quantity)
    {
        return new TradeDomain
        {
            Id = Guid.NewGuid(),
            BuyOrderId = buyOrderId,
            SellOrderId = sellOrderId,
            BuyerId = buyerId,
            SellerId = sellerId,
            SymbolId = symbolId,
            Price = price,
            Quantity = quantity,
            ExecutedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static TradeDomain Create(
        Guid tradeId,
        Guid buyOrderId,
        Guid sellOrderId,
        Guid buyerId,
        Guid sellerId,
        Guid symbolId,
        Price price,
        Quantity quantity,
        DateTime executedAtUtc)
    {
        return new TradeDomain
        {
            Id = tradeId,
            BuyOrderId = buyOrderId,
            SellOrderId = sellOrderId,
            BuyerId = buyerId,
            SellerId = sellerId,
            SymbolId = symbolId,
            Price = price,
            Quantity = quantity,
            ExecutedAt = executedAtUtc,
            CreatedAt = executedAtUtc
        };
    }

    /// <summary>
    /// Gets the total value of the trade (price * quantity).
    /// </summary>
    public decimal GetTotalValue() => Price.Value * Quantity.Value;
}
