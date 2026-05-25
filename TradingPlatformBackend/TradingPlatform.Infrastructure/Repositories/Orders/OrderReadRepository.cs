using Microsoft.EntityFrameworkCore;
using TradingEngine.Application.Common.Models;
using TradingEngine.Application.Features.Common;
using TradingEngine.Application.Features.Orders.Dtos;
using TradingEngine.Application.Interfaces.Orders;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.Enums;
using TradingEngine.Domain.ValueObjects;
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
            .FilterBySymbol(filter.SymbolId)
            .FilterBySearch(filter.Search)
            .FilterBySide(filter.Side)
            .FilterByStatus(filter.Status)
            .SortBy(pagination.GetSortingOptions());

        return await query.ToPagedResultAsync(
            pagination,
            OrderMappers.ToOrderListDto,
            cancellationToken);
    }

    public async Task<OrderSummary> GetOrderSummaryAsync(Guid userId, CancellationToken cancellationToken)
    {
        var query = from o in _dbContext.Orders.AsNoTracking()
                    where o.UserId == userId
                    select o;

        var orders = await query.ToListAsync(cancellationToken);

        var groups = (from o in orders
                      group o by o.Status into g
                      select new
                      {
                          Status = g.Key,
                          Count = g.Count(),
                          Volume = g.Sum(x => (x.Quantity.Value - x.RemainingQuantity.Value) * x.Price.Value)
                      }).ToList();

        var total = groups.Sum(s => s.Count);
        var open = groups.Where(s => s.Status == OrderStatus.Open || s.Status == OrderStatus.PartiallyFilled).Sum(s => s.Count);
        var filled = groups.Where(s => s.Status == OrderStatus.Filled).Sum(s => s.Count);
        var cancelled = groups.Where(s => s.Status == OrderStatus.Cancelled).Sum(s => s.Count);
        var totalVolume = groups.Sum(s => s.Volume);

        return new OrderSummary(total, open, filled, cancelled, totalVolume);
    }

    public async Task<OrderDomain?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await _dbContext.Orders
            .Include(o => o.Symbol)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }
}
