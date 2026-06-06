using TradingEngine.Application.Common;
using TradingEngine.Application.Interfaces.Accounts;
using TradingEngine.Domain.Enums;
using TradingEngine.Domain.ValueObjects;
using TradingEngine.Application.Interfaces;

namespace TradingEngine.Application.Features.Accounts.Commands;

public sealed record AdminDepositCommand(Guid UserId, decimal Amount, Currency Currency) : ICommand<Result>;

public sealed class AdminDepositCommandHandler : ICommandHandler<AdminDepositCommand, Result>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdminDepositCommandHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AdminDepositCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            return Result.Failure("Amount must be greater than zero.");

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var account = await _accountRepository.GetByIdAsync(request.UserId, ct);
            if (account == null)
                return Result.Failure("User account not found.");

            account.Deposit(new Money(request.Amount, request.Currency));

            await _accountRepository.UpdateAsync(account, ct);
            await _unitOfWork.CommitAsync(ct);

            return Result.Success();
        }, cancellationToken);
    }
}
