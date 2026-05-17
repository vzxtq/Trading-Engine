using Microsoft.Extensions.Options;
using TradingEngine.Application.Common;
using TradingEngine.Application.Features.Accounts.Dtos;
using TradingEngine.Application.Interfaces;
using TradingEngine.Application.Interfaces.Accounts;
using TradingEngine.Application.Options;

namespace TradingEngine.Application.Features.Accounts.Commands;

public sealed record LoginCommand(string Email, string Password) : ICommand<Result<LoginResponseDto>>;

public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, Result<LoginResponseDto>>
{
    private readonly IUserIdentityRepository _identityRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly JwtSettings _jwtOptions;

    public LoginCommandHandler(
        IUserIdentityRepository identityRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        IOptions<JwtSettings> jwtOptions)
    {
        _identityRepository = identityRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<Result<LoginResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var identity = await _identityRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (identity == null)
        {
            return Result<LoginResponseDto>.Failure("Invalid email or password.");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, identity.PasswordHash))
        {
            return Result<LoginResponseDto>.Failure("Invalid email or password.");
        }

        var token = _tokenGenerator.GenerateToken(identity);
        var refreshToken = _tokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        identity.UpdateRefreshToken(refreshToken, refreshTokenExpiry);
        await _identityRepository.UpdateAsync(identity, cancellationToken);

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes).ToUnixTimeMilliseconds();

        return Result<LoginResponseDto>.Success(new LoginResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            UserId = identity.UserId,
            Email = request.Email
        });
    }
}