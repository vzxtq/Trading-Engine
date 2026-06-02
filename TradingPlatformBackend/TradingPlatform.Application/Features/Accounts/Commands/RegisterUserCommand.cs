using Microsoft.Extensions.Options;
using TradingEngine.Application.Common;
using TradingEngine.Application.Features.Accounts.Dtos;
using TradingEngine.Application.Interfaces;
using TradingEngine.Application.Interfaces.Accounts;
using TradingEngine.Application.Options;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.Enums;
using TradingEngine.Domain.ValueObjects;

namespace TradingEngine.Application.Features.Accounts.Commands;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    decimal InitialBalance,
    Currency Currency) : ICommand<Result<LoginResponseDto>>;

public sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, Result<LoginResponseDto>>
{
    private readonly IUserIdentityRepository _identityRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly Application.Interfaces.IJwtTokenGenerator _tokenGenerator;
    private readonly JwtSettings _jwtOptions;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUserCommandHandler(
        IUserIdentityRepository identityRepository,
        IAccountRepository accountRepository,
        IPasswordHasher passwordHasher,
        Application.Interfaces.IJwtTokenGenerator tokenGenerator,
        IOptions<JwtSettings> jwtOptions,
        IUnitOfWork unitOfWork)
    {
        _identityRepository = identityRepository;
        _accountRepository = accountRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _jwtOptions = jwtOptions.Value;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LoginResponseDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var existingIdentity = await _identityRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingIdentity != null)
        {
            return Result<LoginResponseDto>.Failure("User with this email already exists.");
        }

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var refreshToken = _tokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var initialBalance = new Money(request.InitialBalance, request.Currency);
            var account = UserAccountDomain.Create(request.Email, request.FirstName, request.LastName, initialBalance);
            await _accountRepository.AddAsync(account, ct);

            var identity = new UserIdentityDomain(account.Id, passwordHash);
            identity.UpdateRefreshToken(refreshToken, refreshTokenExpiry);
            await _identityRepository.AddAsync(identity, ct);

            await _unitOfWork.CommitAsync(ct);

            var token = _tokenGenerator.GenerateToken(identity);
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes).ToUnixTimeMilliseconds();

            return Result<LoginResponseDto>.Success(new LoginResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                UserId = account.Id,
                Email = account.Email
            });
        }, cancellationToken);
    }
}
