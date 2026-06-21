namespace TradingEngine.Application.Features.Orders.Placement;

public sealed record OrderPlacementResult(
    decimal ReservedAmount,
    long EnginePrice,
    long EngineMaxTotalCost);
