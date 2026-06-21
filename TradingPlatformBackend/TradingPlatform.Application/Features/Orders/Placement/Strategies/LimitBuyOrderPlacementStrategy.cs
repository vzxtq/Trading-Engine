using TradingEngine.Application.Common;
using TradingEngine.Application.Interfaces.Accounts;
using TradingEngine.Domain.Enums;
using TradingEngine.MatchingEngine.Scaling;

namespace TradingEngine.Application.Features.Orders.Placement.Strategies;

public sealed class LimitBuyOrderPlacementStrategy
    : BuyOrderPlacementStrategyBase
{
    public LimitBuyOrderPlacementStrategy(
        IAccountRepository accountRepository)
        : base(accountRepository)
    {
    }

    public override OrderSide Side => OrderSide.Buy;
    public override OrderType Type => OrderType.Limit;

    public override async Task<Result<OrderPlacementResult>> ExecuteAsync(
        OrderPlacementContext context,
        CancellationToken cancellationToken)
    {
        var price = context.Price
            ?? throw new InvalidOperationException("Limit order price is required.");

        var maxTotalCost = price.Value * context.Quantity.Value;

        await ReserveFundsAsync(
            context,
            maxTotalCost,
            cancellationToken);

        return Result<OrderPlacementResult>.Success(
            new OrderPlacementResult(
                maxTotalCost,
                price.Value.ToEnginePrice(),
                maxTotalCost.ToEngineNotional()));
    }
}
