using TradingEngine.Domain.ValueObjects;

namespace TradingEngine.MatchingEngine.Commands;

/// <summary>
/// Immutable command envelope consumed by the matching engine.
/// </summary>
public abstract record MatchingEngineCommand
{
    public Guid CommandOutboxId { get; init; }
    public bool IsDurable => CommandOutboxId != Guid.Empty;
    public long SequenceId { get; init; }
    public required Symbol Symbol { get; init; }
    public required Guid SymbolId { get; init; }
    public required long ReceivedAt { get; init; }
}
