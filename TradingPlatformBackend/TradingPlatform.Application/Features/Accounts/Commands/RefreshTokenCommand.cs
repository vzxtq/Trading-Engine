using Microsoft.Extensions.Options;
using TradingEngine.Application.Common;
using TradingEngine.Application.Features.Accounts.Dtos;
using TradingEngine.Application.Interfaces;
using TradingEngine.Application.Interfaces.Accounts;
using TradingEngine.Application.Options;

namespace TradingEngine.Application.Features.Accounts.Commands;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<Result<LoginResponseDto>>;

public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, Result<LoginResponseDto>>
{
    private readonly IUserIdentityRepository _identityRepository;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly JwtSettings _jwtOptions;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(
        IUserIdentityRepository identityRepository,
        IJwtTokenGenerator tokenGenerator,
        IOptions<JwtSettings> jwtOptions,
        IUnitOfWork unitOfWork)
    {
        _identityRepository = identityRepository;
        _tokenGenerator = tokenGenerator;
        _jwtOptions = jwtOptions.Value;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LoginResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var identity = await _identityRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (identity == null || identity.RefreshTokenExpiresAt < DateTime.UtcNow)
        {
            return Result<LoginResponseDto>.Failure("Invalid or expired refresh token.");
        }

        var token = _tokenGenerator.GenerateToken(identity);
        var newRefreshToken = _tokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        identity.UpdateRefreshToken(newRefreshToken, refreshTokenExpiry);
        await _identityRepository.UpdateAsync(identity, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes).ToUnixTimeMilliseconds();

        return Result<LoginResponseDto>.Success(new LoginResponseDto
        {
            Token = token,
            RefreshToken = newRefreshToken,
            ExpiresAt = expiresAt,
            UserId = identity.UserId
        });
    }
}