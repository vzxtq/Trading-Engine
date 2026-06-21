using TradingEngine.Domain.Enums;
using TradingEngine.Domain.ValueObjects;
using TradingEngine.MatchingEngine.Models;

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
}

public sealed record AddOrderCommand : MatchingEngineCommand
{
    public required Guid OrderId { get; init; }
    public required Guid UserId { get; init; }
    public required long Price { get; init; }
    public required long Quantity { get; init; }
    public required OrderSide Side { get; init; }
    public required OrderType Type { get; init; }
    public required long MaxTotalCost { get; init; }
    public required long ReceivedAt { get; init; }
}

public sealed record CancelOrderCommand : MatchingEngineCommand
{
    public required Guid OrderId { get; init; }
}

/// <summary>
/// command to capture a consistent snapshot of an order book.
/// Processed inside the shard worker thread to avoid locking.
/// </summary>
public sealed record SnapshotOrderBookCommand : MatchingEngineCommand
{
    public required TaskCompletionSource<OrderBookSnapshot> Completion { get; init; }
}