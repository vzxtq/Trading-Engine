namespace TradingEngine.MatchingEngine.Commands;

public sealed record CancelOrderCommand : MatchingEngineCommand
{
    public required Guid OrderId { get; init; }
}
