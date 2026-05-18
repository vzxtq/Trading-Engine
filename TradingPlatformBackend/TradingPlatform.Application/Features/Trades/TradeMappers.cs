using System.Linq.Expressions;
using TradingEngine.Application.Common;
using TradingEngine.Application.Features.Trades.Dtos;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.Enums;

namespace TradingEngine.Application.Features.Trades;

public static class TradeMappers
{
    public static Expression<Func<TradeDomain, TradeDto>> ToTradeDto(Guid userId) =>
        t => new TradeDto
        {
            TradeId = t.Id,
            Symbol = t.Symbol.Name,
            Price = t.Price.Value,
            Quantity = t.Quantity.Value,
            Side = t.BuyerId == userId ? OrderSide.Buy : OrderSide.Sell,
            ExecutedAt = t.ExecutedAt.ToUnixTimeMs()
        };
}
