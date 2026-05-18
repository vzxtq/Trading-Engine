using System.Text.Json.Serialization;
using TradingEngine.Application.Features.Orders.Repositories;
using TradingEngine.Application.Interfaces.Orders;
using TradingEngine.Application.Interfaces.Accounts;
using TradingEngine.MatchingEngine.Interfaces;
using TradingEngine.MatchingEngine.Commands;
using TradingEngine.Application.Common;
using TradingEngine.Application.Features.Orders.Dtos;
using TradingEngine.Domain.ValueObjects;
using TradingEngine.Domain.Enums;
using TradingEngine.Application.Interfaces.Positions;

namespace TradingEngine.Application.Features.Orders.Commands;

/// <summary>
/// Command to cancel an existing order.
/// </summary>
public class CancelOrderCommand : ICommand<Result<CancelOrderResponseDto>>
{
    public Guid OrderId { get; set; }

    [JsonIgnore]
    public Guid UserId { get; set; }
}

public sealed class CancelOrderCommandHandler : ICommandHandler<CancelOrderCommand, Result<CancelOrderResponseDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderReadRepository _orderReadRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly IMatchingEngineQueue _engineQueue;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        IOrderReadRepository orderReadRepository,
        IAccountRepository accountRepository,
        IPositionRepository positionRepository,
        IMatchingEngineQueue engineQueue)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _orderReadRepository = orderReadRepository ?? throw new ArgumentNullException(nameof(orderReadRepository));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _positionRepository = positionRepository ?? throw new ArgumentNullException(nameof(positionRepository));
        _engineQueue = engineQueue ?? throw new ArgumentNullException(nameof(engineQueue));
    }

    public async Task<Result<CancelOrderResponseDto>> Handle(
        CancelOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orderReadRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result<CancelOrderResponseDto>.Failure("Order not found");
        }

        if (order.UserId != request.UserId)
        {
            return Result<CancelOrderResponseDto>.Failure("Order does not belong to user");
        }

        // Release reserved funds for Buy orders or reserved quantity for Sell orders
        if (order.Side == OrderSide.Buy)
        {
            var account = await _accountRepository.GetByIdAsync(order.UserId, cancellationToken);
            if (account != null)
            {
                var releaseAmount = new Money(order.Price.Value * order.RemainingQuantity.Value, account.Balance.Currency);
                account.ReleaseReservedFunds(releaseAmount);
                await _accountRepository.UpdateAsync(account, cancellationToken);
            }
        }
        else if (order.Side == OrderSide.Sell)
        {
            var position = await _positionRepository.GetUserPositionForSymbolAsync(order.UserId, order.Symbol.Name, cancellationToken);
            if (position != null)
            {
                position.ReleaseReserved(order.RemainingQuantity);
                await _positionRepository.UpdateAsync(position, cancellationToken);
            }
        }

        // Update domain state
        order.Cancel();
        await _orderRepository.UpdateAsync(order, cancellationToken);

        // Notify engine to remove from book
        var cancelCommand = new MatchingEngine.Commands.CancelOrderCommand
        {
            OrderId = order.Id,
            Symbol = new Symbol(order.Symbol.Name),
            SymbolId = order.SymbolId
        };

        await _engineQueue.EnqueueAsync(cancelCommand, cancellationToken);

        return Result<CancelOrderResponseDto>.Success(new CancelOrderResponseDto
        {
            OrderId = order.Id,
            Message = "Cancel request accepted"
        });
    }
}
