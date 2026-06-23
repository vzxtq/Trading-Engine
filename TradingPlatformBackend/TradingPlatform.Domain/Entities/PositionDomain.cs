using TradingEngine.Domain.Common;
using TradingEngine.Domain.ValueObjects;

namespace TradingEngine.Domain.Entities;

/// <summary>
/// Represents a user's position in a particular stock symbol.
/// Tracks quantity held for a given symbol.
/// </summary>
public class PositionDomain : BaseEntity
{
    public Guid UserId { get; private set; }
    public Symbol SymbolValue { get; private set; } = null!;
    public Quantity Quantity { get; private set; } = null!;
    public Quantity ReservedQuantity { get; private set; } = null!;
    public decimal AverageCost { get; private set; }

    public Quantity AvailableQuantity => new Quantity(Quantity.Value - ReservedQuantity.Value);

    private PositionDomain()
    { }

    /// <summary>
    /// Creates a new position or returns null if quantity becomes zero.
    /// </summary>
    public static PositionDomain? Create(Guid userId, Symbol symbolValue, Quantity quantity, decimal averageCost)
    {
        if (quantity.Value <= 0)
            return null;

        return new PositionDomain
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SymbolValue = symbolValue,
            Quantity = quantity,
            ReservedQuantity = new Quantity(0),
            AverageCost = averageCost
        };
    }

    /// <summary>
    /// Increases the position by adding to quantity.
    /// </summary>
    public void Add(Quantity quantity, decimal price)
    {
        var totalCost = (Quantity.Value * AverageCost) + (quantity.Value * price);
        var totalQuantity = Quantity.Value + quantity.Value;
        Quantity = new Quantity(totalQuantity);
        AverageCost = totalCost / totalQuantity;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Decreases the position by reducing quantity.
    /// </summary>
    public void Reduce(Quantity quantity)
    {
        if (quantity.IsGreaterThan(Quantity))
            throw new InvalidOperationException("Cannot reduce position more than current quantity");

        Quantity = Quantity.Subtract(quantity);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reserve(Quantity quantity)
    {
        if (quantity.IsGreaterThan(AvailableQuantity))
            throw new InvalidOperationException("Insufficient available position quantity to reserve.");

        ReservedQuantity = new Quantity(ReservedQuantity.Value + quantity.Value);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReleaseReserved(Quantity quantity)
    {
        if (quantity.IsGreaterThan(ReservedQuantity))
            throw new InvalidOperationException("Cannot release more than reserved quantity.");

        ReservedQuantity = new Quantity(ReservedQuantity.Value - quantity.Value);
        UpdatedAt = DateTime.UtcNow;
    }

    public void CommitReserved(Quantity quantity)
    {
        if (quantity.IsGreaterThan(ReservedQuantity))
            throw new InvalidOperationException("Cannot commit more than reserved quantity.");

        ReservedQuantity = new Quantity(ReservedQuantity.Value - quantity.Value);
        Quantity = Quantity.Subtract(quantity);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculates the unrealized P&L based on current market price.
    /// </summary>
    public decimal CalculateUnrealizedPnL(Price currentPrice)
    {
        return (currentPrice.Value - AverageCost) * Quantity.Value;
    }
}
