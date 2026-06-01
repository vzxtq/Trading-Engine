using TradingEngine.Domain.Entities;

namespace TradingEngine.Application.Interfaces.Orders;

/// <summary>
/// Repository for managing OrderDomain entities.
/// </summary>
public interface IOrderRepository
{
    Task AddAsync(OrderDomain order, CancellationToken cancellationToken);
    Task UpdateAsync(OrderDomain order, CancellationToken cancellationToken);
    Task<OrderDomain?> GetByIdempotencyKeyAsync(Guid userId, string idempotencyKey, CancellationToken cancellationToken);
}
