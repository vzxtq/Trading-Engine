using TradingEngine.Domain.Enums;

namespace TradingEngine.Application.Interfaces.OrderCommands.Dtos;

public sealed record AddOrderCommandOutboxDto(
    Guid OrderId,
    Guid UserId,
    Guid SymbolId,
    string Symbol,
    long Price,
    long Quantity,
    OrderSide Side,
    OrderType Type,
    long MaxTotalCost);
