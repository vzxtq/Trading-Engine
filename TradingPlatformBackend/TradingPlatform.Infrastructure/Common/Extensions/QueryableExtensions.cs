using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TradingEngine.Application.Common.Models;

namespace TradingEngine.Infrastructure.Common.Extensions;

public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PaginatedQuery pagination,
        CancellationToken ct)
    {
        return await query.ToPagedResultAsync(
            pagination.NormalizedPage,
            pagination.NormalizedPageSize,
            ct);
    }

    public static async Task<PagedResult<TResult>> ToPagedResultAsync<TSource, TResult>(
        this IQueryable<TSource> query,
        PaginatedQuery pagination,
        Expression<Func<TSource, TResult>> selector,
        CancellationToken ct)
    {
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((pagination.NormalizedPage - 1) * pagination.NormalizedPageSize)
            .Take(pagination.NormalizedPageSize)
            .Select(selector)
            .ToListAsync(ct);

        return new PagedResult<TResult>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.NormalizedPage,
            PageSize = pagination.NormalizedPageSize,
        };
    }

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }
}
