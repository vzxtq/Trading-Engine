namespace TradingEngine.MatchingEngine.Models;

public sealed record SelfTradePreventionEvent(
    Guid TakerOrderId,
    Guid MakerOrderId,
    Guid UserId,
    SelfTradePreventionPolicy Policy,
    string Reason);