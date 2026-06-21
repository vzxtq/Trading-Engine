using TradingEngine.Application.Common;
using TradingEngine.Application.Interfaces.Accounts;
using TradingEngine.Application.Interfaces.OrderPlacement;
using TradingEngine.Domain.Enums;
using TradingEngine.Domain.ValueObjects;

namespace TradingEngine.Application.Features.Orders.Placement.Strategies;

public abstract class BuyOrderPlacementStrategyBase : IOrderPlacementStrategy
{
    private readonly IAccountRepository _accountRepository;

    protected BuyOrderPlacementStrategyBase(
        IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public abstract OrderSide Side { get; }
    public abstract OrderType Type { get; }

    public abstract Task<Result<OrderPlacementResult>> ExecuteAsync(
        OrderPlacementContext context,
        CancellationToken cancellationToken);

    protected async Task ReserveFundsAsync(
        OrderPlacementContext context,
        decimal amount,
        CancellationToken cancellationToken)
    {
        context.Account.ReserveFunds(
            new Money(amount, context.Account.Balance.Currency));

        await _accountRepository.UpdateAsync(
            context.Account,
            cancellationToken);
    }
}
