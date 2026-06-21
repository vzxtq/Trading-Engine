using System.Text.Json.Serialization;
using TradingEngine.Application.Common;
using TradingEngine.Application.Features.Orders.Dtos;
using TradingEngine.Application.Interfaces.Orders;
using TradingEngine.Application.Interfaces;
using TradingEngine.Application.Interfaces.OrderCommands;
using TradingEngine.Domain.Enums;

namespace TradingEngine.Application.Features.Orders.Commands;

public class CancelOrderCommand : ICommand<Result<CancelOrderResponseDto>>
{
    public Guid OrderId { get; set; }

    [JsonIgnore]
    public Guid UserId { get; set; }
}

public sealed class CancelOrderCommandHandler : ICommandHandler<CancelOrderCommand, Result<CancelOrderResponseDto>>
{
    private readonly IOrderReadRepository _orderReadRepository;
    private readonly IOrderCommandOutboxRepository _commandOutbox;
    private readonly IUnitOfWork _unitOfWork;

    public CancelOrderCommandHandler(
        IOrderReadRepository orderReadRepository,
        IOrderCommandOutboxRepository commandOutbox,
        IUnitOfWork unitOfWork)
    {
        _orderReadRepository = orderReadRepository ?? throw new ArgumentNullException(nameof(orderReadRepository));
        _commandOutbox = commandOutbox ?? throw new ArgumentNullException(nameof(commandOutbox));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<CancelOrderResponseDto>> Handle(
        CancelOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orderReadRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
            return Result<CancelOrderResponseDto>.Failure("Order not found");

        if (order.UserId != request.UserId)
            return Result<CancelOrderResponseDto>.Failure("Order does not belong to user");

        if (order.Status == OrderStatus.Filled ||
            order.Status == OrderStatus.Cancelled ||
            order.Status == OrderStatus.Rejected ||
            order.Status == OrderStatus.PartiallyFilledCancelled)
        {
            return Result<CancelOrderResponseDto>.Failure($"Cannot cancel order with status {order.Status}");
        }

        var alreadyPending = await _commandOutbox.HasPendingCancelAsync(order.Id, cancellationToken);
        if (alreadyPending)
            return Result<CancelOrderResponseDto>.Success(new CancelOrderResponseDto(order.Id));

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await _commandOutbox.AddCancelAsync(
                    order.Id,
                    order.SymbolId,
                    order.Symbol.Name,
                    ct);
                await _unitOfWork.CommitAsync(ct);
                return true;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<CancelOrderResponseDto>.Failure(
                $"Failed to queue cancellation: {ex.Message}");
        }

        return Result<CancelOrderResponseDto>.Success(
            new CancelOrderResponseDto(order.Id));
    }
}
