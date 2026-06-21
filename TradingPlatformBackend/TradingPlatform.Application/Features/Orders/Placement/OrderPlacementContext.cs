using TradingEngine.Domain.Entities;
using TradingEngine.Domain.ValueObjects;

namespace TradingEngine.Application.Features.Orders.Placement;

public sealed record OrderPlacementContext(
    Guid UserId,
    UserAccountDomain Account,
    Symbol Symbol,
    Price? Price,
    Quantity Quantity);
