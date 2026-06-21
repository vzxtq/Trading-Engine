using TradingEngine.Application.Common;
using TradingEngine.Application.Interfaces.Accounts;
using TradingEngine.Domain.Enums;
using TradingEngine.MatchingEngine.Interfaces;
using TradingEngine.MatchingEngine.Scaling;

namespace TradingEngine.Application.Features.Orders.Placement.Strategies;

public sealed class MarketBuyOrderPlacementStrategy
    : BuyOrderPlacementStrategyBase
{
    private readonly IOrderBookSnapshotProvider _snapshotProvider;

    public MarketBuyOrderPlacementStrategy(
        IAccountRepository accountRepository,
        IOrderBookSnapshotProvider snapshotProvider)
        : base(accountRepository)
    {
        _snapshotProvider = snapshotProvider;
    }

    public override OrderSide Side => OrderSide.Buy;
    public override OrderType Type => OrderType.Market;

    public override async Task<Result<OrderPlacementResult>> ExecuteAsync(
        OrderPlacementContext context,
        CancellationToken cancellationToken)
    {
        var snapshot = await _snapshotProvider.GetSnapshotAsync(
            context.Symbol,
            cancellationToken);

        var remainingQuantity = context.Quantity.Value.ToEngineQuantity();
        long estimatedCost = 0;

        foreach (var ask in snapshot.Asks)
        {
            var fillQuantity = Math.Min(
                remainingQuantity,
                ask.TotalQuantity);

            estimatedCost = checked(
                estimatedCost + fillQuantity * ask.Price);
            remainingQuantity -= fillQuantity;

            if (remainingQuantity == 0)
                break;
        }

        if (remainingQuantity > 0)
        {
            return Result<OrderPlacementResult>.Failure(
                "Insufficient liquidity for market order");
        }

        var maxTotalCost = checked(
                estimatedCost + estimatedCost / 20)
            .ToDomainNotional();

        await ReserveFundsAsync(
            context,
            maxTotalCost,
            cancellationToken);

        return Result<OrderPlacementResult>.Success(
            new OrderPlacementResult(
                maxTotalCost,
                0,
                maxTotalCost.ToEngineNotional()));
    }
}
