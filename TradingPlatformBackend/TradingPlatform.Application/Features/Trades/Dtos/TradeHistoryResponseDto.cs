using TradingEngine.Application.Common.Models;
using TradingEngine.Application.Features.Trades.Dtos;

namespace TradingEngine.Application.Features.Trades.Dtos;

public record TradeHistoryResponseDto(
    PagedResult<TradeDto> Trades,
    SortingOptions? Sorting);
