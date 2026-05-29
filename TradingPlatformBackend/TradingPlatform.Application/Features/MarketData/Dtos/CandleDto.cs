namespace TradingEngine.Application.Features.MarketData.Dtos;

public class CandleDto
{
    public long Time { get; set; }     // Unix seconds (not ms) for lightweight-charts
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
}
