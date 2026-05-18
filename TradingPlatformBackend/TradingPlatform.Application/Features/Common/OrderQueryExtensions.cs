using TradingEngine.Application.Common.Models;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.Enums;

namespace TradingEngine.Application.Features.Common;

public static class OrderQueryExtensions
{
    public static IQueryable<OrderDomain> FilterByUserId(this IQueryable<OrderDomain> query, Guid userId)
    {
        return query.Where(o => o.UserId == userId);
    }

    public static IQueryable<OrderDomain> FilterBySymbol(this IQueryable<OrderDomain> query, Guid? symbolId)
    {
        if (!symbolId.HasValue)
            return query;

        return query.Where(o => o.SymbolId == symbolId.Value);
    }

    public static IQueryable<OrderDomain> FilterBySide(this IQueryable<OrderDomain> query, OrderSide? side)
    {
        return side.HasValue ? query.Where(o => o.Side == side.Value) : query;
    }

    public static IQueryable<OrderDomain> FilterByStatus(this IQueryable<OrderDomain> query, OrderStatus? status)
    {
        return status.HasValue ? query.Where(o => o.Status == status.Value) : query;
    }

    public static IQueryable<OrderDomain> FilterBySearch(this IQueryable<OrderDomain> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return query;

        var searchTerm = search.Trim().ToUpper();
        return query.Where(o => o.Symbol.Name.Contains(searchTerm));
    }

    public static IQueryable<OrderDomain> SortBy(this IQueryable<OrderDomain> query, SortingOptions? options)
    {
        if (options == null)
            return query.OrderByDescending(o => o.CreatedAt);

        var isDescending = options.Direction != SortingDirection.Ascending;

        return options.Column.ToLower() switch
        {
            "symbol" => isDescending ? query.OrderByDescending(o => o.Symbol.Name) : query.OrderBy(o => o.Symbol.Name),
            "price" => isDescending ? query.OrderByDescending(o => o.Price) : query.OrderBy(o => o.Price),
            "createdat" => isDescending ? query.OrderByDescending(o => o.CreatedAt) : query.OrderBy(o => o.CreatedAt),
            _ => query.OrderByDescending(o => o.CreatedAt)
        };
    }
}
