using TradingEngine.Domain.Entities;
using TradingEngine.Domain.Enums;

namespace TradingEngine.Application.Features.Common;

public static class OrderQueryExtensions
{
    public static IQueryable<OrderDomain> FilterByUserId(this IQueryable<OrderDomain> query, Guid userId)
    {
        return query.Where(o => o.UserId == userId);
    }

    public static IQueryable<OrderDomain> FilterBySymbol(this IQueryable<OrderDomain> query, string? symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return query;

        var symbolValue = symbol.Trim().ToUpper();
        return query.Where(o => o.Symbol.Name == symbolValue);
    }

    public static IQueryable<OrderDomain> FilterBySide(this IQueryable<OrderDomain> query, OrderSide? side)
    {
        return side.HasValue ? query.Where(o => o.Side == side.Value) : query;
    }

    public static IQueryable<OrderDomain> FilterByStatus(this IQueryable<OrderDomain> query, OrderStatus? status)
    {
        return status.HasValue ? query.Where(o => o.Status == status.Value) : query;
    }

    public static IQueryable<OrderDomain> OrderByNewest(this IQueryable<OrderDomain> query)
    {
        return query.OrderByDescending(o => o.CreatedAt);
    }
}
