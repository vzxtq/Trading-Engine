using MediatR;
using System.Text.Json.Serialization;
using TradingEngine.Application.Common;
using TradingEngine.Application.Common.Models;
using TradingEngine.Application.Features.Orders.Dtos;
using TradingEngine.Application.Interfaces.Orders;

namespace TradingEngine.Application.Features.Orders.Queries;

public record GetOrdersByUserIdQuery : PaginatedQuery, IQuery<Result<OrderListResponseDto>>
{
    [JsonIgnore]
    public Guid UserId { get; set; }
    public OrderFilterDto Filter { get; set; } = new();
}

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersByUserIdQuery, Result<OrderListResponseDto>>
{
    private readonly IOrderReadRepository _orderReadRepository;

    public GetOrdersQueryHandler(IOrderReadRepository orderReadRepository)
    {
        _orderReadRepository = orderReadRepository;
    }

    public async Task<Result<OrderListResponseDto>> Handle(GetOrdersByUserIdQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderReadRepository.GetOrdersAsync(
            request.UserId,
            request.Filter,
            request,
            cancellationToken);

        var summary = await _orderReadRepository.GetOrderSummaryAsync(request.UserId, cancellationToken);

        var summaryDto = new OrderSummaryDto(
            summary.TotalOrders,
            summary.OpenOrders,
            summary.FilledOrders,
            summary.CancelledOrders,
            summary.TotalVolume,
            summary.FillRate,
            summary.CancelledRate);

        return Result<OrderListResponseDto>.Success(new OrderListResponseDto(orders, summaryDto, request.GetSortingOptions()));
    }
}
