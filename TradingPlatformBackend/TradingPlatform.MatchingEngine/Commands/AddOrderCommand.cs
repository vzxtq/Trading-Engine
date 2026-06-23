using TradingEngine.Domain.Enums;

namespace TradingEngine.MatchingEngine.Commands;

public sealed record AddOrderCommand : MatchingEngineCommand
{
    public required Guid OrderId { get; init; }
    public required Guid UserId { get; init; }
    public required long Price { get; init; }
    public required long Quantity { get; init; }
    public required OrderSide Side { get; init; }
    public required OrderType Type { get; init; }
    public required long MaxTotalCost { get; init; }
}
