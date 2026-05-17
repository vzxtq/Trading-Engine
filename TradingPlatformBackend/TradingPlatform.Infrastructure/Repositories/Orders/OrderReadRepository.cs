using Microsoft.EntityFrameworkCore;
using TradingEngine.Application.Common;
using TradingEngine.Application.Common.Models;
using TradingEngine.Application.Features.Accounts.Dtos;
using TradingEngine.Application.Features.Common;
using TradingEngine.Application.Features.Orders.Dtos;
using TradingEngine.Application.Interfaces.Orders;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.Enums;
using TradingEngine.Infrastructure.Common.Extensions;
using TradingEngine.Infrastructure.Persistence;

namespace TradingEngine.Infrastructure.Repositories.Orders;

public class OrderReadRepository : IOrderReadRepository
{
    private readonly TradingDbContext _dbContext;

    public OrderReadRepository(TradingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<OrderListDto>> GetOrdersAsync(
        Guid userId,
        OrderFilterDto filter,
        PaginatedQuery pagination,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Orders
            .AsNoTracking()
            .FilterByUserId(userId)
            .FilterBySymbol(filter.Symbol)
            .FilterBySide(filter.Side)
            .FilterByStatus(filter.Status)
            .OrderByNewest();

        return await query.ToPagedResultAsync(
            pagination,
            OrderMappers.ToOrderListDto,
            cancellationToken);
    }

    public async Task<OrderDomain?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await _dbContext.Orders
            .Include(o => o.Symbol)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }
}
