using TradingEngine.Application.Common;
using TradingEngine.Application.Features.MarketData.Dtos;
using TradingEngine.Application.Interfaces;

namespace TradingEngine.Application.Features.MarketData.Queries;

public record GetCandlesQuery : IQuery<Result<IReadOnlyList<CandleDto>>>
{
    public string Symbol { get; init; } = string.Empty;
    public string Interval { get; init; } = "1m";
    public int Limit { get; init; } = 100;
}

public sealed class GetCandlesQueryHandler : IQueryHandler<GetCandlesQuery, Result<IReadOnlyList<CandleDto>>>
{
    private readonly IMarketDataProvider _marketDataProvider;

    public GetCandlesQueryHandler(IMarketDataProvider marketDataProvider)
    {
        _marketDataProvider = marketDataProvider;
    }

    public async Task<Result<IReadOnlyList<CandleDto>>> Handle(
        GetCandlesQuery request,
        CancellationToken cancellationToken)
    {
        var candles = await _marketDataProvider.GetCandlesAsync(
            request.Symbol,
            request.Interval,
            request.Limit,
            cancellationToken);

        return Result<IReadOnlyList<CandleDto>>.Success(candles);
    }
}
