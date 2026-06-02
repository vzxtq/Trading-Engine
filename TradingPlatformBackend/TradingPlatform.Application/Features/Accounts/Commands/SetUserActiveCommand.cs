using TradingEngine.Application.Common;
using TradingEngine.Application.Interfaces;
using TradingEngine.Application.Interfaces.Accounts;

namespace TradingEngine.Application.Features.Accounts.Commands;

public sealed record SetUserActiveCommand(Guid UserId, bool IsActive) : ICommand<Result>;

public sealed class SetUserActiveCommandHandler : ICommandHandler<SetUserActiveCommand, Result>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetUserActiveCommandHandler(
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetUserActiveCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (account == null)
        {
            return Result.Failure("Account not found.");
        }

        if (request.IsActive)
        {
            account.Activate();
        }
        else
        {
            account.Deactivate();
        }

        await _accountRepository.UpdateAsync(account, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
