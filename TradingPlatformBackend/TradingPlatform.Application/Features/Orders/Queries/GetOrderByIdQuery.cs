using TradingEngine.Application.Common;
using TradingEngine.Application.Features.Orders.Dtos;
using TradingEngine.Application.Interfaces.Orders;

namespace TradingEngine.Application.Features.Orders.Queries;

/// <summary>
/// Query to retrieve an order by its ID.
/// </summary>
public class GetOrderByIdQuery : IQuery<Result<OrderDto>>
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
}

public class GetOrderByIdQueryHandler : IQueryHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    private readonly IOrderReadRepository _orderReadRepository;

    public GetOrderByIdQueryHandler(IOrderReadRepository orderReadRepository)
    {
        _orderReadRepository = orderReadRepository;
    }

    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderReadRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null) return Result<OrderDto>.Failure("Order not found");

        if (order.UserId != request.UserId) return Result<OrderDto>.Failure("Unauthorized access to order.");

        return Result<OrderDto>.Success(new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            SymbolName = order.Symbol.Name,
            Currency = order.Symbol.Currency,
            Price = order.Price != null ? order.Price.Value : 0,
            Quantity = order.Quantity.Value,
            RemainingQuantity = order.RemainingQuantity.Value,
            Side = order.Side,
            Status = order.Status,
            CreatedAt = order.CreatedAt.ToUnixTimeMs(),
            UpdatedAt = order.UpdatedAt.ToUnixTimeMs()
        });
    }
}