using TradingEngine.Domain.Enums;

namespace TradingEngine.Application.Features.Trades.Dtos;

public record TradeFilterDto
{
    public Guid? SymbolId { get; init; }
    public OrderSide? Side { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}
