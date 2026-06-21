using TradingEngine.Application.Interfaces.OrderCommands.Dtos;

namespace TradingEngine.Application.Interfaces.OrderCommands;

public interface IOrderCommandOutboxRepository
{
    Task AddOrderAsync(
        AddOrderCommandOutboxDto command,
        CancellationToken cancellationToken);

    Task AddCancelAsync(
        Guid orderId,
        Guid symbolId,
        string symbol,
        CancellationToken cancellationToken);

    Task<bool> HasPendingCancelAsync(Guid orderId, CancellationToken cancellationToken);
}
