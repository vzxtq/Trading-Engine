using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TradingEngine.Application.Features.MarketData.Dtos;
using TradingEngine.Application.Interfaces;

namespace TradingEngine.Infrastructure.ExternalServices;

public sealed class BinanceMarketDataProvider : IMarketDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BinanceMarketDataProvider> _logger;

    private static readonly Dictionary<string, string?> SymbolMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BTCUSD"]  = "BTCUSDT",
        ["ETHUSD"]  = "ETHUSDT",
        ["SOLUSD"]  = "SOLUSDT", // TODO 
    };

    public BinanceMarketDataProvider(HttpClient httpClient, ILogger<BinanceMarketDataProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CandleDto>> GetCandlesAsync(
        string symbol,
        string interval,
        int limit,
        CancellationToken cancellationToken)
    {
        if (!SymbolMap.TryGetValue(symbol, out var binanceSymbol) || binanceSymbol is null)
            return Array.Empty<CandleDto>();

        var url = $"https://api.binance.com/api/v3/klines?symbol={binanceSymbol}&interval={interval}&limit={limit}";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<JsonElement[][]>(url, cancellationToken);
            if (response is null) return Array.Empty<CandleDto>();

            return response.Select(k => new CandleDto
            {
                Time   = k[0].GetInt64() / 1000,
                Open   = decimal.Parse(k[1].GetString()!, CultureInfo.InvariantCulture),
                High   = decimal.Parse(k[2].GetString()!, CultureInfo.InvariantCulture),
                Low    = decimal.Parse(k[3].GetString()!, CultureInfo.InvariantCulture),
                Close  = decimal.Parse(k[4].GetString()!, CultureInfo.InvariantCulture),
                Volume = decimal.Parse(k[5].GetString()!, CultureInfo.InvariantCulture),
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch candles from Binance for {Symbol}", binanceSymbol);
            return Array.Empty<CandleDto>();
        }
    }

    public async Task<OrderBookDto> GetOrderBookAsync(
        string symbol,
        int limit,
        CancellationToken cancellationToken)
    {
        var empty = new OrderBookDto { Symbol = symbol };

        if (!SymbolMap.TryGetValue(symbol, out var binanceSymbol) || binanceSymbol is null)
            return empty;

        var url = $"https://api.binance.com/api/v3/depth?symbol={binanceSymbol}&limit={limit}";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<BinanceDepthResponse>(url, cancellationToken);
            if (response is null) return empty;

            static OrderBookEntryDto ParseEntry(string[] entry) => new()
            {
                Price    = decimal.Parse(entry[0], CultureInfo.InvariantCulture),
                Quantity = decimal.Parse(entry[1], CultureInfo.InvariantCulture),
            };

            return new OrderBookDto
            {
                Symbol = symbol,
                Bids   = response.Bids.Select(ParseEntry).ToList(),
                Asks   = response.Asks.Select(ParseEntry).ToList(),
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch order book from Binance for {Symbol}", binanceSymbol);
            return empty;
        }
    }
}
