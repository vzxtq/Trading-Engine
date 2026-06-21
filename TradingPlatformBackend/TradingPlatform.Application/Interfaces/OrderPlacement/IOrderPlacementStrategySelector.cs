using TradingEngine.Domain.Enums;

namespace TradingEngine.Application.Interfaces.OrderPlacement;

public interface IOrderPlacementStrategySelector
{
    IOrderPlacementStrategy Select(OrderSide side, OrderType type);
}
