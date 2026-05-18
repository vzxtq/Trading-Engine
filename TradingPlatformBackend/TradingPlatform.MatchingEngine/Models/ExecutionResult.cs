using TradingEngine.Domain.ValueObjects;

namespace TradingEngine.MatchingEngine.Models;

/// <summary>
/// Deterministic outcome returned by the in-memory matching engine.
/// No side effects, pure data contract between engine and the rest of the system.
/// </summary>
public abstract record ExecutionResult
{
    public required Symbol Symbol { get; init; }
    public required Guid SymbolId { get; init; }
    public required long SequenceId { get; init; }
    public required long EngineTimestamp { get; init; }

    public sealed record Accepted : ExecutionResult
    {
        public required IReadOnlyList<ExecutedTrade> Trades { get; init; }
        public required IReadOnlyList<OrderStateChange> StateChanges { get; init; }

        public bool HasTrades => Trades.Count > 0;
    }

    public sealed record Rejected : ExecutionResult
    {
        public required string Reason { get; init; }
    }
}
