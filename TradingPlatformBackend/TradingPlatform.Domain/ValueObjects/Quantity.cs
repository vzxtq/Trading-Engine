namespace TradingEngine.Domain.ValueObjects;

/// <summary>
/// Represents a quantity of shares.
/// Ensures quantity is always positive.
/// </summary>
public class Quantity : IEquatable<Quantity>, IComparable<Quantity>
{
    public decimal Value { get; private set; }

    public Quantity()
    { }

    public Quantity(decimal value)
    {
        if (value < 0)
            throw new ArgumentException("Quantity cannot be negative", nameof(value));

        Value = value;
    }

    public Quantity Add(Quantity other) => new(Value + other.Value);

    public Quantity Subtract(Quantity other)
    {
        var result = Value - other.Value;
        if (result < 0)
            throw new InvalidOperationException("Cannot subtract more than available quantity");
        return new Quantity(result);
    }

    public bool IsGreaterThanOrEqual(Quantity other) => Value >= other.Value;

    public bool IsGreaterThan(Quantity other) => Value > other.Value;

    public bool IsLessThan(Quantity other) => Value < other.Value;

    public bool IsEqual(Quantity other) => Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as Quantity);

    public bool Equals(Quantity? other) => other is not null && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public int CompareTo(Quantity? other) => other is null ? 1 : Value.CompareTo(other.Value);
}