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

        if (order.Status == OrderStatus.Filled ||
            order.Status == OrderStatus.Cancelled ||
            order.Status == OrderStatus.Rejected)
        {
            return Result<CancelOrderResponseDto>.Failure($"Cannot cancel order with status {order.Status}");
        }


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
