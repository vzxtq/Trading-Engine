using TradingEngine.Application.Common;
using TradingEngine.Application.Interfaces;
using TradingEngine.Application.Interfaces.Accounts;

namespace TradingEngine.Application.Features.Accounts.Commands;

public sealed record LogoutCommand : ICommand<Result>;

public sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand, Result>
{
    private readonly IUserIdentityRepository _identityRepository;
    private readonly IUserResolverService _userResolverService;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutCommandHandler(
        IUserIdentityRepository identityRepository,
        IUserResolverService userResolverService,
        IUnitOfWork unitOfWork)
    {
        _identityRepository = identityRepository;
        _userResolverService = userResolverService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var userId = _userResolverService.GetUserId();
        var identity = await _identityRepository.GetByUserIdAsync(userId, cancellationToken);
        
        if (identity != null)
        {
            identity.InvalidateRefreshToken();
            await _identityRepository.UpdateAsync(identity, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }

        return Result.Success();
    }
}