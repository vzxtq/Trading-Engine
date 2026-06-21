using TradingEngine.Application.Common;
using TradingEngine.Application.Interfaces.OrderPlacement;
using TradingEngine.Application.Interfaces.Positions;
using TradingEngine.Domain.Enums;
using TradingEngine.MatchingEngine.Scaling;

namespace TradingEngine.Application.Features.Orders.Placement.Strategies;

public abstract class SellOrderPlacementStrategyBase : IOrderPlacementStrategy
{
    private readonly IPositionRepository _positionRepository;

    protected SellOrderPlacementStrategyBase(
        IPositionRepository positionRepository)
    {
        _positionRepository = positionRepository;
    }

    public OrderSide Side => OrderSide.Sell;
    public abstract OrderType Type { get; }

    public async Task<Result<OrderPlacementResult>> ExecuteAsync(
        OrderPlacementContext context,
        CancellationToken cancellationToken)
    {
        var position = await _positionRepository.GetUserPositionForSymbolAsync(
            context.UserId,
            context.Symbol.Value,
            cancellationToken);

        if (position is null
            || position.AvailableQuantity.IsLessThan(context.Quantity))
        {
            return Result<OrderPlacementResult>.Failure(
                "Insufficient position");
        }

        position.Reserve(context.Quantity);
        await _positionRepository.UpdateAsync(position, cancellationToken);

        return Result<OrderPlacementResult>.Success(
            new OrderPlacementResult(
                context.Quantity.Value,
                context.Price?.Value.ToEnginePrice() ?? 0,
                0));
    }
}
