using Microsoft.EntityFrameworkCore;
using TradingEngine.Application.Features.Orders.Repositories;
using TradingEngine.Infrastructure.Persistence;
using TradingEngine.Domain.Entities;
using TradingEngine.Application.Interfaces.Orders;

namespace TradingEngine.Infrastructure.Repositories.Orders;

public sealed class OrderRepository : IOrderRepository
{
    private readonly TradingDbContext _dbContext;

    public OrderRepository(TradingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OrderDomain?> GetByIdempotencyKeyAsync(Guid userId, string idempotencyKey, CancellationToken cancellationToken)
    {
        return await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.UserId == userId && o.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task AddAsync(OrderDomain order, CancellationToken cancellationToken)
    {
        await _dbContext.Orders.AddAsync(order, cancellationToken);
    }

    public Task UpdateAsync(OrderDomain order, CancellationToken cancellationToken)
    {
        _dbContext.Orders.Update(order);
        return Task.CompletedTask;
    }
}
