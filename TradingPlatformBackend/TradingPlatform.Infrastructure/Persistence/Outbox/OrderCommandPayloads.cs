using TradingEngine.Domain.Enums;

namespace TradingEngine.Infrastructure.Persistence.Outbox;

public sealed record AddOrderCommandPayload(
    Guid OrderId,
    Guid UserId,
    Guid SymbolId,
    string Symbol,
    long Price,
    long Quantity,
    OrderSide Side,
    OrderType Type,
    long MaxTotalCost);

public sealed record CancelOrderCommandPayload(
    Guid OrderId,
    Guid SymbolId,
    string Symbol);
