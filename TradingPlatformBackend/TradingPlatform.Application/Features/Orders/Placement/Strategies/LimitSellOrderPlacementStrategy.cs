using TradingEngine.Application.Interfaces.Positions;
using TradingEngine.Domain.Enums;

namespace TradingEngine.Application.Features.Orders.Placement.Strategies;

public sealed class LimitSellOrderPlacementStrategy
    : SellOrderPlacementStrategyBase
{
    public LimitSellOrderPlacementStrategy(
        IPositionRepository positionRepository)
        : base(positionRepository)
    {
    }

    public override OrderType Type => OrderType.Limit;
}
