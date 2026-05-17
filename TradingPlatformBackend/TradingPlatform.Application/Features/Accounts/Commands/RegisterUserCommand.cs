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

    public RegisterUserCommandHandler(
        IUserIdentityRepository identityRepository,
        IAccountRepository accountRepository,
        IPasswordHasher passwordHasher,
        Application.Interfaces.IJwtTokenGenerator tokenGenerator,
        IOptions<JwtSettings> jwtOptions)
    {
        _identityRepository = identityRepository;
        _accountRepository = accountRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<Result<LoginResponseDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var existingIdentity = await _identityRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingIdentity != null)
        {
            return Result<LoginResponseDto>.Failure("User with this email already exists.");
        }

        // 1. Create UserAccount
        var initialBalance = new Money(request.InitialBalance, request.Currency);
        var account = UserAccountDomain.Create(request.Email, request.FirstName, request.LastName, initialBalance);
        await _accountRepository.AddAsync(account, cancellationToken);

        // 2. Create UserIdentity
        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var identity = new UserIdentityDomain(account.Id, passwordHash);
        
        var refreshToken = _tokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        identity.UpdateRefreshToken(refreshToken, refreshTokenExpiry);

        await _identityRepository.AddAsync(identity, cancellationToken);

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
    }
}
