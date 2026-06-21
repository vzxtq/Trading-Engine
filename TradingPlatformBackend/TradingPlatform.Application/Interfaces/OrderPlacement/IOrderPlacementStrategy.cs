using TradingEngine.Application.Common;
using TradingEngine.Application.Features.Orders.Placement;
using TradingEngine.Domain.Enums;

namespace TradingEngine.Application.Interfaces.OrderPlacement;

public interface IOrderPlacementStrategy
{
    OrderSide Side { get; }
    OrderType Type { get; }

    Task<Result<OrderPlacementResult>> ExecuteAsync(
        OrderPlacementContext context,
        CancellationToken cancellationToken);
}
