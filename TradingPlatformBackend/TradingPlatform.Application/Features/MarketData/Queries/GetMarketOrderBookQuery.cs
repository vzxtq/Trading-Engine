using TradingEngine.Application.Common;
using TradingEngine.Application.Features.MarketData.Dtos;
using TradingEngine.Application.Interfaces;

namespace TradingEngine.Application.Features.MarketData.Queries;

public record GetMarketOrderBookQuery : IQuery<Result<OrderBookDto>>
{
    public string Symbol { get; init; } = string.Empty;
    public int Limit { get; init; } = 20;
}

public sealed class GetMarketOrderBookQueryHandler : IQueryHandler<GetMarketOrderBookQuery, Result<OrderBookDto>>
{
    private readonly IMarketDataProvider _marketDataProvider;

    public GetMarketOrderBookQueryHandler(IMarketDataProvider marketDataProvider)
    {
        _marketDataProvider = marketDataProvider;
    }

    public async Task<Result<OrderBookDto>> Handle(
        GetMarketOrderBookQuery request,
        CancellationToken cancellationToken)
    {
        var orderBook = await _marketDataProvider.GetOrderBookAsync(
            request.Symbol,
            request.Limit,
            cancellationToken);

        return Result<OrderBookDto>.Success(orderBook);
    }
}
