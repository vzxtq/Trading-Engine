using TradingEngine.Application.Common.Models;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.Enums;

namespace TradingEngine.Application.Features.Common;

public static class TradeQueryExtensions
{
    public static IQueryable<TradeDomain> FilterByUserId(this IQueryable<TradeDomain> query, Guid userId)
    {
        return query.Where(t => t.BuyerId == userId || t.SellerId == userId);
    }

    public static IQueryable<TradeDomain> FilterBySymbol(this IQueryable<TradeDomain> query, Guid? symbolId)
    {
        if (!symbolId.HasValue)
            return query;

        return query.Where(t => t.SymbolId == symbolId.Value);
    }

    public static IQueryable<TradeDomain> FilterBySide(this IQueryable<TradeDomain> query, Guid userId, OrderSide? side)
    {
        if (!side.HasValue)
            return query;

        return side.Value == OrderSide.Buy 
            ? query.Where(t => t.BuyerId == userId) 
            : query.Where(t => t.SellerId == userId);
    }

    public static IQueryable<TradeDomain> FilterByDateRange(this IQueryable<TradeDomain> query, DateTime? from, DateTime? to)
    {
        if (from.HasValue)
            query = query.Where(t => t.ExecutedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.ExecutedAt <= to.Value);

        return query;
    }

    public static IQueryable<TradeDomain> SortBy(this IQueryable<TradeDomain> query, SortingOptions? options)
    {
        if (options == null)
            return query.OrderByDescending(t => t.ExecutedAt);

        var isDescending = options.Direction != SortingDirection.Ascending;

        return options.Column.ToLower() switch
        {
            "symbol" => isDescending ? query.OrderByDescending(t => t.Symbol.Name) : query.OrderBy(t => t.Symbol.Name),
            "price" => isDescending ? query.OrderByDescending(t => t.Price.Value) : query.OrderBy(t => t.Price.Value),
            "quantity" => isDescending ? query.OrderByDescending(t => t.Quantity.Value) : query.OrderBy(t => t.Quantity.Value),
            "executedat" => isDescending ? query.OrderByDescending(t => t.ExecutedAt) : query.OrderBy(t => t.ExecutedAt),
            _ => query.OrderByDescending(t => t.ExecutedAt)
        };
    }
}
