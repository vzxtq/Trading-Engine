using Microsoft.EntityFrameworkCore;
using TradingEngine.Application.Features.Orders.Dtos;
using TradingEngine.Application.Features.Orders.Repositories;
using TradingEngine.Infrastructure.Persistence;
using TradingEngine.Domain.ValueObjects;
using TradingEngine.Application.Common;

namespace TradingEngine.Infrastructure.Repositories.Orders;

public sealed class OrderBookReadRepository : IOrderBookReadRepository
{
    private readonly TradingDbContext _dbContext;

    public OrderBookReadRepository(TradingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OrderBookDto> GetOrderBookAsync(Symbol symbol, CancellationToken cancellationToken)
    {
        var buyOrders = await _dbContext.Orders
            .Where(o => o.Symbol.Name == symbol.Value && o.Side == Domain.Enums.OrderSide.Buy)
            .OrderByDescending(o => EF.Property<decimal>(o, "Price"))
            .ThenBy(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        var sellOrders = await _dbContext.Orders
            .Where(o => o.Symbol.Name == symbol.Value && o.Side == Domain.Enums.OrderSide.Sell)
            .OrderBy(o => EF.Property<decimal>(o, "Price"))
            .ThenBy(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        return new OrderBookDto
        {
            Symbol = symbol.Value,
            BuyOrders = buyOrders.Select(MapOrder).ToList(),
            SellOrders = sellOrders.Select(MapOrder).ToList()
        };
    }

    private static OrderDto MapOrder(Domain.Entities.OrderDomain o) => new()
    {
        Id = o.Id,
        UserId = o.UserId,
        SymbolName = o.Symbol.Name,

        Price = o.Price.Value,
        Quantity = o.Quantity.Value,
        RemainingQuantity = o.RemainingQuantity.Value,
        Side = o.Side,
        Status = o.Status,
        CreatedAt = o.CreatedAt.ToUnixTimeMs(),
        UpdatedAt = o.UpdatedAt.ToUnixTimeMs()
    };
}
