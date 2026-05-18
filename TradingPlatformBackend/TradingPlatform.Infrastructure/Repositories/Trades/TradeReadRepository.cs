using Microsoft.EntityFrameworkCore;
using TradingEngine.Application.Common.Models;
using TradingEngine.Application.Features.Common;
using TradingEngine.Application.Features.Trades;
using TradingEngine.Application.Features.Trades.Dtos;
using TradingEngine.Application.Interfaces.Trades;
using TradingEngine.Infrastructure.Common.Extensions;
using TradingEngine.Infrastructure.Persistence;

namespace TradingEngine.Infrastructure.Repositories.Trades;

public sealed class TradeReadRepository : ITradeReadRepository
{
    private readonly TradingDbContext _dbContext;

    public TradeReadRepository(TradingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<TradeDto>> GetByUserIdAsync(
        Guid userId, 
        TradeFilterDto filter,
        PaginatedQuery pagination,
        CancellationToken ct)
    {
        var query = _dbContext.Trades
            .AsNoTracking()
            .Include(t => t.Symbol)
            .FilterByUserId(userId)
            .FilterBySymbol(filter.SymbolId)
            .FilterBySide(userId, filter.Side)
            .FilterByDateRange(filter.From, filter.To)
            .SortBy(pagination.GetSortingOptions());

        return await query.ToPagedResultAsync(
            pagination,
            TradeMappers.ToTradeDto(userId),
            ct);
    }
}
