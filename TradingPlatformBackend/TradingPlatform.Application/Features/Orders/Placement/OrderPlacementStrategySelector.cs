using TradingEngine.Application.Interfaces.OrderPlacement;
using TradingEngine.Domain.Enums;

namespace TradingEngine.Application.Features.Orders.Placement;

public sealed class OrderPlacementStrategySelector : IOrderPlacementStrategySelector
{
    private readonly IReadOnlyDictionary<
        (OrderSide Side, OrderType Type),
        IOrderPlacementStrategy> _strategies;

    public OrderPlacementStrategySelector(
        IEnumerable<IOrderPlacementStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(
            strategy => (strategy.Side, strategy.Type));
    }

    public IOrderPlacementStrategy Select(OrderSide side, OrderType type)
    {
        if (_strategies.TryGetValue((side, type), out var strategy))
            return strategy;

        throw new InvalidOperationException(
            $"Order placement strategy for {side}/{type} is not registered.");
    }
}
