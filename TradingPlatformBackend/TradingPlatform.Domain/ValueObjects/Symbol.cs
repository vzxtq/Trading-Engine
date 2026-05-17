namespace TradingEngine.Domain.ValueObjects;

/// <summary>
/// Represents a stock symbol with validation.
/// Example: AAPL, GOOGL, MSFT
/// </summary>
public class Symbol : IEquatable<Symbol>
{
    private const int MaxLength = 10;
    private const int MinLength = 1;

    public string Value { get; private set; }

    public Symbol()
    { }

    public Symbol(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Symbol cannot be empty", nameof(value));

        var trimmed = value.Trim().ToUpperInvariant();

        if (trimmed.Length < MinLength || trimmed.Length > MaxLength)
            throw new ArgumentException($"Symbol must be between {MinLength} and {MaxLength} characters", nameof(value));

        if (!trimmed.All(char.IsLetter))
            throw new ArgumentException("Symbol must contain only letters", nameof(value));

        Value = trimmed;
    }

    public override bool Equals(object? obj) => Equals(obj as Symbol);

    public bool Equals(Symbol? other) => other is not null && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(Symbol? left, Symbol? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Symbol? left, Symbol? right) => !(left == right);
}