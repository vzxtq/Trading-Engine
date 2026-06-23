using TradingEngine.MatchingEngine.Models;

namespace TradingEngine.MatchingEngine.Commands;

/// <summary>
/// Command to capture a consistent snapshot of an order book.
/// Processed inside the shard worker thread to avoid locking.
/// </summary>
public sealed record SnapshotOrderBookCommand : MatchingEngineCommand
{
    public required TaskCompletionSource<OrderBookSnapshot> Completion { get; init; }
}
