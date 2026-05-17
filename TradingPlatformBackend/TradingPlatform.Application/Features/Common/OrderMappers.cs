using System.Linq.Expressions;
using TradingEngine.Application.Common;
using TradingEngine.Application.Features.Accounts.Dtos;
using TradingEngine.Application.Features.Orders.Dtos;
using TradingEngine.Domain.Entities;

namespace TradingEngine.Application.Features.Common;

public static class OrderMappers
{
    public static Expression<Func<OrderDomain, OrderListDto>> ToOrderListDto => order => new OrderListDto
    {
        Id = order.Id,
        SymbolName = order.Symbol.Name,
        Currency = order.Symbol.Currency,
        Side = order.Side,
        Type = "Limit", // TODO: Should be implemented enum
        Price = new MoneyDto
        {
            Amount = order.Price.Value,
            Currency = order.Symbol.Currency
        },
        Quantity = order.Quantity.Value,
        FilledQuantity = order.FilledQuantity,
        Status = order.Status,
        CreatedAt = order.CreatedAt.ToUnixTimeMs(),
        UserId = order.UserId,
        RemainingQuantity = order.RemainingQuantity.Value,
        UpdatedAt = order.UpdatedAt.ToUnixTimeMs()
    };
}
