using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TradingEngine.Application.Interfaces.OrderCommands;
using TradingEngine.Application.Interfaces.OrderCommands.Dtos;
using TradingEngine.Infrastructure.Persistence;
using TradingEngine.Infrastructure.Persistence.Outbox;

namespace TradingEngine.Infrastructure.Repositories.OrderCommands;

public sealed class OrderCommandOutboxRepository : IOrderCommandOutboxRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly TradingDbContext _dbContext;

    public OrderCommandOutboxRepository(TradingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddOrderAsync(
        AddOrderCommandOutboxDto command,
        CancellationToken cancellationToken)
    {
        var payload = new AddOrderCommandPayload(
            command.OrderId,
            command.UserId,
            command.SymbolId,
            command.Symbol,
            command.Price,
            command.Quantity,
            command.Side,
            command.Type,
            command.MaxTotalCost);

        var entry = OrderCommandOutboxEntry.Create(
            command.SymbolId,
            command.OrderId,
            OrderCommandType.AddOrder,
            JsonSerializer.Serialize(payload, SerializerOptions));

        await _dbContext.OrderCommandOutbox.AddAsync(entry, cancellationToken);
    }

    public async Task AddCancelAsync(
        Guid orderId,
        Guid symbolId,
        string symbol,
        CancellationToken cancellationToken)
    {
        var payload = new CancelOrderCommandPayload(orderId, symbolId, symbol);
        var entry = OrderCommandOutboxEntry.Create(
            symbolId,
            orderId,
            OrderCommandType.CancelOrder,
            JsonSerializer.Serialize(payload, SerializerOptions));

        await _dbContext.OrderCommandOutbox.AddAsync(entry, cancellationToken);
    }

    public Task<bool> HasPendingCancelAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return _dbContext.OrderCommandOutbox.AnyAsync(
            x => x.ActiveCancellationOrderId == orderId,
            cancellationToken);
    }
}
