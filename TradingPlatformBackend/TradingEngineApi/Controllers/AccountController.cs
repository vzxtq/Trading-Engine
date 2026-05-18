using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingEngine.Application.Features.Accounts.Commands;
using TradingEngine.Application.Features.Accounts.Queries;
using TradingEngine.Application.Features.Positions.Queries;
using TradingEngine.Application.Features.Trades.Queries;
using TradingEngine.Application.Interfaces;
using TradingEngineApi.Extensions;

namespace TradingEngine.Api.Controllers;

[Route("api/[controller]")]
public class AccountsController : ApiController
{
    private readonly IUserResolverService _userResolverService;

    public AccountsController(IMediator mediator, IUserResolverService userResolverService) : base(mediator)
    {
        _userResolverService = userResolverService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.ToActionResult();
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var result = await _mediator.Send(new LogoutCommand(), ct);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var query = new GetAccountByIdQuery { AccountId = id };
        var result = await _mediator.Send(query, ct);

        return result.ToActionResult();
    }

    [Authorize]
    [HttpGet("positions")]
    public async Task<IActionResult> GetMyPositions(CancellationToken ct)
    {
        var query = new GetPositionsByUserIdQuery { UserId = _userResolverService.GetUserId() };
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpGet("trades")]
    public async Task<IActionResult> GetMyTradeHistory([FromQuery] GetTradeHistoryQuery query, CancellationToken ct)
    {
        query.UserId = _userResolverService.GetUserId();
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpPut]
    public async Task<IActionResult> UpdateAccount([FromBody] UpdateAccountCommand command, CancellationToken ct)
    {
        command.UserId = _userResolverService.GetUserId();

        var result = await _mediator.Send(command, ct);
        return result.ToActionResult();
    }
}
