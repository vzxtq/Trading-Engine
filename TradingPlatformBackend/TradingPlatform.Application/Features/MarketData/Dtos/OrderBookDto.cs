namespace TradingEngine.Application.Features.MarketData.Dtos;

public class OrderBookEntryDto
{
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
}

public class OrderBookDto
{
    public string Symbol { get; set; } = string.Empty;
    public IReadOnlyList<OrderBookEntryDto> Bids { get; set; } = [];
    public IReadOnlyList<OrderBookEntryDto> Asks { get; set; } = [];
}
