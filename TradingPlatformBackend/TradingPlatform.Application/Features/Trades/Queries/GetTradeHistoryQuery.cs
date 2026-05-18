using System.Text.Json.Serialization;
using TradingEngine.Application.Common;
using TradingEngine.Application.Common.Models;
using TradingEngine.Application.Features.Trades.Dtos;
using TradingEngine.Application.Interfaces.Trades;

namespace TradingEngine.Application.Features.Trades.Queries;

public record GetTradeHistoryQuery : PaginatedQuery, IQuery<Result<TradeHistoryResponseDto>>
{
    [JsonIgnore]
    public Guid UserId { get; set; }
    public TradeFilterDto Filter { get; init; } = new();
}

public sealed class GetTradeHistoryQueryHandler : IQueryHandler<GetTradeHistoryQuery, Result<TradeHistoryResponseDto>>
{
    private readonly ITradeReadRepository _tradeReadRepository;

    public GetTradeHistoryQueryHandler(ITradeReadRepository tradeReadRepository)
    {
        _tradeReadRepository = tradeReadRepository;
    }

    public async Task<Result<TradeHistoryResponseDto>> Handle(GetTradeHistoryQuery request, CancellationToken cancellationToken)
    {
        var pagedTrades = await _tradeReadRepository.GetByUserIdAsync(
            request.UserId,
            request.Filter,
            request,
            cancellationToken);

        return Result<TradeHistoryResponseDto>.Success(new TradeHistoryResponseDto(pagedTrades, request.GetSortingOptions()));
    }
}
