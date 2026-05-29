using MediatR;
using Microsoft.AspNetCore.Mvc;
using TradingEngine.Application.Features.MarketData.Queries;
using TradingEngineApi.Extensions;

namespace TradingEngine.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MarketDataController : ApiController
{
    public MarketDataController(IMediator mediator) : base(mediator) { }

    /// <summary>Returns historical OHLCV candlestick data — proxies Binance klines.</summary>
    [HttpGet("candles")]
    public async Task<IActionResult> GetCandles(
        [FromQuery] GetCandlesQuery query,
        CancellationToken ct)
    {
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }

    /// <summary>Returns current order book depth — proxies Binance /api/v3/depth.</summary>
    [HttpGet("orderbook")]
    public async Task<IActionResult> GetOrderBook(
        [FromQuery] GetMarketOrderBookQuery query,
        CancellationToken ct)
    {
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }
}
