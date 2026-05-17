using MediatR;
using System.Text.Json.Serialization;
using TradingEngine.Application.Common;
using TradingEngine.Application.Common.Models;
using TradingEngine.Application.Features.Orders.Dtos;
using TradingEngine.Application.Interfaces.Orders;

namespace TradingEngine.Application.Features.Orders.Queries;

public record GetOrdersByUserIdQuery : PaginatedQuery, IQuery<Result<PagedResult<OrderListDto>>>
{
    [JsonIgnore]
    public Guid UserId { get; set; }
    public OrderFilterDto Filter { get; set; } = new();
}

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersByUserIdQuery, Result<PagedResult<OrderListDto>>>
{
    private readonly IOrderReadRepository _orderReadRepository;

    public GetOrdersQueryHandler(IOrderReadRepository orderReadRepository)
    {
        _orderReadRepository = orderReadRepository;
    }

    public async Task<Result<PagedResult<OrderListDto>>> Handle(GetOrdersByUserIdQuery request, CancellationToken cancellationToken)
    {
        var result = await _orderReadRepository.GetOrdersAsync(
            request.UserId,
            request.Filter,
            request,
            cancellationToken);

        return Result<PagedResult<OrderListDto>>.Success(result);
    }
}
