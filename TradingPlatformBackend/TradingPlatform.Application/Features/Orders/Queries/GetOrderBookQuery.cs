using TradingEngine.Application.Common;
using TradingEngine.Application.Features.Orders.Repositories;
using TradingEngine.Application.Features.Orders.Dtos;
using TradingEngine.Domain.ValueObjects;

namespace TradingEngine.Application.Features.Orders.Queries;

public class GetOrderBookQuery : IQuery<Result<OrderBookDto>>
{
    public string Symbol { get; set; } = string.Empty;
}

public sealed class GetOrderBookQueryHandler : IQueryHandler<GetOrderBookQuery, Result<OrderBookDto>>
{
    private readonly IOrderBookReadRepository _readRepository;

    public GetOrderBookQueryHandler(IOrderBookReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<OrderBookDto>> Handle(GetOrderBookQuery request, CancellationToken cancellationToken)
    {
        var symbol = new Symbol(request.Symbol);
        var book = await _readRepository.GetOrderBookAsync(symbol, cancellationToken);
        return Result<OrderBookDto>.Success(book);
    }
}
