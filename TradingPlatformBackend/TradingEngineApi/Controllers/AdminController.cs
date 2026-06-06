using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingEngine.Application.Features.Accounts.Commands;
using TradingEngine.Application.Interfaces;
using TradingEngine.Application.Interfaces.Accounts;
using TradingEngine.Domain.Enums;
using TradingEngineApi.Extensions;

namespace TradingEngine.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
public class AdminController : ApiController
{
    private readonly IUserResolverService _userResolverService;
    private readonly IAccountRepository _accountRepository;

    public AdminController(
        IMediator mediator, 
        IUserResolverService userResolverService, 
        IAccountRepository accountRepository) : base(mediator)
    {
        _userResolverService = userResolverService;
        _accountRepository = accountRepository;
    }

    public sealed record AdminDepositRequest(decimal Amount, Currency Currency);

    [HttpPost("{userId:guid}/deposit")]
    public async Task<IActionResult> Deposit(Guid userId, [FromBody] AdminDepositRequest request, CancellationToken ct)
    {
        var adminId = _userResolverService.GetUserId();
        var adminAccount = await _accountRepository.GetByIdAsync(adminId, ct);
        
        if (adminAccount == null || !adminAccount.Email.Equals("admin@tradingplatform.com", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var command = new AdminDepositCommand(userId, request.Amount, request.Currency);
        var result = await _mediator.Send(command, ct);
        return result.ToActionResult();
    }
}
