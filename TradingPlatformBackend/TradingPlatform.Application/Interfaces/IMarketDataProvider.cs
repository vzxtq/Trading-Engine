using TradingEngine.Application.Features.MarketData.Dtos;

namespace TradingEngine.Application.Interfaces;

public interface IMarketDataProvider
{
    Task<IReadOnlyList<CandleDto>> GetCandlesAsync(
        string symbol,
        string interval,
        int limit,
        CancellationToken cancellationToken);

    Task<OrderBookDto> GetOrderBookAsync(
        string symbol,
        int limit,
        CancellationToken cancellationToken);
}
