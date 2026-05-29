namespace TradingEngine.Application.Features.MarketData.Dtos
{
    public class BinanceDepthResponse
    {
        public string[][] Bids { get; set; } = [];
        public string[][] Asks { get; set; } = [];
    }
}
