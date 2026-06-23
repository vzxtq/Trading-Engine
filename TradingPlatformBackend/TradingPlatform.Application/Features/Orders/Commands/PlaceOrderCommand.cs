using TradingEngine.Application.Features.Orders.Repositories;
using TradingEngine.Application.Interfaces.Orders;
using TradingEngine.Application.Interfaces.Accounts;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.Enums;
using TradingEngine.Application.Common;
using TradingEngine.Application.Features.Orders.Dtos;
using TradingEngine.Application.Features.Orders.Placement;
using TradingEngine.Domain.ValueObjects;
using TradingEngine.Application.Interfaces;
using TradingEngine.Application.Interfaces.Symbols;
using TradingEngine.Application.Interfaces.OrderCommands;
using TradingEngine.Application.Interfaces.OrderCommands.Dtos;
using TradingEngine.Application.Interfaces.OrderPlacement;
using TradingEngine.MatchingEngine.Scaling;

namespace TradingEngine.Application.Features.Orders.Commands;

/// <summary>
/// Command to place a new order in the system.
/// </summary>
public class PlaceOrderCommand : ICommand<Result<PlaceOrderResponseDto>>
{
    public string Symbol { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public decimal Quantity { get; set; }
    public OrderSide Side { get; set; }
    public OrderType Type { get; set; }
    public string? IdempotencyKey { get; set; }
}

public sealed class PlaceOrderCommandHandler : ICommandHandler<PlaceOrderCommand, Result<PlaceOrderResponseDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderCommandOutboxRepository _commandOutbox;
    private readonly IAccountRepository _accountRepository;
    private readonly IUserResolverService _userResolver;
    private readonly ISymbolReadRepository _symbolRepository;
    private readonly IOrderPlacementStrategySelector _strategySelector;
    private readonly IUnitOfWork _unitOfWork;

    public PlaceOrderCommandHandler(
        IOrderRepository orderRepository,
        IOrderCommandOutboxRepository commandOutbox,
        IAccountRepository accountRepository,
        IUserResolverService userResolver,
        ISymbolReadRepository symbolRepository,
        IOrderPlacementStrategySelector strategySelector,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _commandOutbox = commandOutbox;
        _accountRepository = accountRepository;
        _userResolver = userResolver;
        _symbolRepository = symbolRepository;
        _strategySelector = strategySelector;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PlaceOrderResponseDto>> Handle(
        PlaceOrderCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _userResolver.GetUserId();

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existingOrder = await _orderRepository.GetByIdempotencyKeyAsync(userId, request.IdempotencyKey, cancellationToken);
            if (existingOrder != null)
            {
                return Result<PlaceOrderResponseDto>.Success(new PlaceOrderResponseDto
                {
                    OrderId = existingOrder.Id,
                    Status = existingOrder.Status,
                    Message = "Order already placed (idempotent)"
                });
            }
        }

        var symbolEntity = await _symbolRepository.GetSymbolByNameAsync(request.Symbol, cancellationToken);
        if (symbolEntity == null)
            return Result<PlaceOrderResponseDto>.Failure("Symbol not found");

        var symbolValue = new Symbol(request.Symbol);
        var price = request.Type == OrderType.Limit ? new Price(request.Price!.Value) : null;
        var quantity = new Quantity(request.Quantity);

        try
        {
            var strategy = _strategySelector.Select(
                request.Side,
                request.Type);

            return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var account = await _accountRepository.GetByIdAsync(
                    userId,
                    ct);

                if (account is null)
                {
                    return Result<PlaceOrderResponseDto>.Failure(
                        "Account not found");
                }

                var placementContext = new OrderPlacementContext(
                    userId,
                    account,
                    symbolValue,
                    price,
                    quantity);

                var placement = await strategy.ExecuteAsync(
                    placementContext,
                    ct);

                if (placement.IsFailure)
                {
                    return Result<PlaceOrderResponseDto>.Failure(
                        placement.Errors.ToArray());
                }

                var placementResult = placement.Value!;
                var order = OrderDomain.Create(
                    userId,
                    symbolEntity.Id,
                    price,
                    quantity,
                    request.Side,
                    request.Type,
                    placementResult.ReservedAmount,
                    request.IdempotencyKey);

                await _orderRepository.AddAsync(order, ct);

                await _commandOutbox.AddOrderAsync(
                    new AddOrderCommandOutboxDto(
                        order.Id,
                        userId,
                        symbolEntity.Id,
                        symbolValue.Value,
                        placementResult.EnginePrice,
                        quantity.Value.ToEngineQuantity(),
                        request.Side,
                        request.Type,
                        placementResult.EngineMaxTotalCost),
                    ct);

                await _unitOfWork.CommitAsync(ct);

                return Result<PlaceOrderResponseDto>.Success(new PlaceOrderResponseDto
                {
                    OrderId = order.Id,
                    Status = OrderStatus.Open,
                    Message = "Order queued for matching"
                });
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<PlaceOrderResponseDto>.Failure($"Failed to place order: {ex.Message}");
        }
    }
}

