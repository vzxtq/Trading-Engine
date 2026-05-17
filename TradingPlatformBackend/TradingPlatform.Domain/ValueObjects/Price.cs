namespace TradingEngine.Domain.ValueObjects;

/// <summary>
/// Represents a stock price.
/// Ensures price is always positive and has reasonable precision.
/// </summary>
public class Price : IEquatable<Price>, IComparable<Price>
{
    public decimal Value { get; private set; }

    public Price()
    { }

    public Price(decimal value)
    {
        if (value <= 0)
            throw new ArgumentException("Price must be greater than zero", nameof(value));

        if (value > 999999.99m)
            throw new ArgumentException("Price exceeds maximum allowed value", nameof(value));

        Value = Math.Round(value, 2);
    }

    public bool IsGreaterThanOrEqual(Price other) => Value >= other.Value;

    public bool IsGreaterThan(Price other) => Value > other.Value;

    public bool IsLessThan(Price other) => Value < other.Value;

    public bool IsEqual(Price other) => Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as Price);

    public bool Equals(Price? other) => other is not null && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public int CompareTo(Price? other) => other is null ? 1 : Value.CompareTo(other.Value);

    public static bool operator ==(Price? left, Price? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Price? left, Price? right) => !(left == right);

    public static bool operator <(Price? left, Price? right)
    {
        if (left is null || right is null) return false;
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(Price? left, Price? right)
    {
        if (left is null || right is null) return false;
        return left.CompareTo(right) > 0;
    }
}