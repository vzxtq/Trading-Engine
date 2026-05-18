using TradingEngine.Application.Common.Models;
using TradingEngine.Application.Features.Trades.Dtos;

namespace TradingEngine.Application.Interfaces.Trades;

public interface ITradeReadRepository
{
    Task<PagedResult<TradeDto>> GetByUserIdAsync(
        Guid userId,
        TradeFilterDto filter,
        PaginatedQuery pagination,
        CancellationToken ct);
}
