using TradingEngine.Domain.Enums;

namespace TradingEngine.MatchingEngine.Models;

/// <summary>
/// A single matched trade between a taker and a maker.
/// Timestamp is supplied by the caller to keep the engine deterministic.
/// </summary>
public sealed record ExecutedTrade(
    Guid TradeId,
    Guid BuyOrderId,
    Guid SellOrderId,
    Guid BuyerId,
    Guid SellerId,
    Guid SymbolId,
    long Price,
    long Quantity,
    long ExecutedAt,
    OrderSide AggressorSide);