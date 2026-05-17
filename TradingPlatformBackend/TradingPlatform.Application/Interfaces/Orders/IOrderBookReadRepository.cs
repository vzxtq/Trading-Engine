using TradingEngine.Application.Features.Orders.Dtos;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.ValueObjects;

namespace TradingEngine.Application.Features.Orders.Repositories;

public interface IOrderBookReadRepository
{
    Task<OrderBookDto> GetOrderBookAsync(Symbol symbol, CancellationToken cancellationToken);
}
