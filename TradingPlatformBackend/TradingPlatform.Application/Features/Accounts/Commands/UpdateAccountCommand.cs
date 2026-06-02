using System.Text.Json.Serialization;
using TradingEngine.Application.Common;
using TradingEngine.Application.Interfaces;
using TradingEngine.Application.Interfaces.Accounts;

namespace TradingEngine.Application.Features.Accounts.Commands;

public sealed record UpdateAccountCommand : ICommand<Result>
{
    [JsonIgnore]
    public Guid UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
}

public sealed class UpdateAccountCommandHandler : ICommandHandler<UpdateAccountCommand, Result>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUserIdentityRepository _identityRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAccountCommandHandler(
        IAccountRepository accountRepository,
        IUserIdentityRepository identityRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _identityRepository = identityRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (account == null)
        {
            return Result.Failure("Account not found.");
        }

        if (request.FirstName != null || request.LastName != null || request.Email != null)
        {
            var firstName = request.FirstName ?? account.FirstName;
            var lastName = request.LastName ?? account.LastName;
            var email = request.Email ?? account.Email;

            account.Update(firstName, lastName, email);
        }

        if (request.NewPassword != null)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            {
                return Result.Failure("Current password is required to set a new password.");
            }

            var identity = await _identityRepository.GetByUserIdAsync(request.UserId, cancellationToken);
            if (identity == null)
            {
                return Result.Failure("User identity not found.");
            }

            if (!_passwordHasher.VerifyPassword(request.CurrentPassword, identity.PasswordHash))
            {
                return Result.Failure("Current password is incorrect.");
            }

            var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            identity.UpdatePassword(newPasswordHash);
            await _identityRepository.UpdateAsync(identity, cancellationToken);
        }

        await _accountRepository.UpdateAsync(account, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
