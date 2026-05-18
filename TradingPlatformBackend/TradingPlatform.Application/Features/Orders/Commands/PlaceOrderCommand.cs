using TradingEngine.Application.Features.Orders.Repositories;
using TradingEngine.Application.Interfaces.Orders;
using TradingEngine.Application.Interfaces.Accounts;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.Enums;
using TradingEngine.MatchingEngine.Interfaces;
using TradingEngine.MatchingEngine.Commands;
using TradingEngine.Application.Common;
using TradingEngine.Application.Features.Orders.Dtos;
using TradingEngine.Domain.ValueObjects;
using TradingEngine.Application.Interfaces;
using TradingEngine.Application.Interfaces.Symbols;
using TradingEngine.Application.Interfaces.Positions;

namespace TradingEngine.Application.Features.Orders.Commands;

/// <summary>
/// Command to place a new order in the system.
/// </summary>
public class PlaceOrderCommand : ICommand<Result<PlaceOrderResponseDto>>
{
    public string Symbol { get; set; } = string.Empty;
    public long Price { get; set; }
    public long Quantity { get; set; }
    public OrderSide Side { get; set; }
}

public sealed class PlaceOrderCommandHandler : ICommandHandler<PlaceOrderCommand, Result<PlaceOrderResponseDto>>
{
    private readonly IMatchingEngineQueue _queue;
    private readonly IOrderRepository _orderRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly IUserResolverService _userResolver;
    private readonly ISymbolReadRepository _symbolRepository;

    public PlaceOrderCommandHandler(
        IMatchingEngineQueue queue,
        IOrderRepository orderRepository,
        IAccountRepository accountRepository,
        IPositionRepository positionRepository,
        IUserResolverService userResolver,
        ISymbolReadRepository symbolRepository)
    {
        _queue = queue;
        _orderRepository = orderRepository;
        _accountRepository = accountRepository;
        _positionRepository = positionRepository;
        _userResolver = userResolver;
        _symbolRepository = symbolRepository;
    }

    public async Task<Result<PlaceOrderResponseDto>> Handle(
        PlaceOrderCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _userResolver.GetUserId();
        
        try
        {
            var symbolEntity = await _symbolRepository.GetSymbolByNameAsync(request.Symbol, cancellationToken);
            if (symbolEntity == null)
                return Result<PlaceOrderResponseDto>.Failure("Symbol not found");

            var symbolValue = new Symbol(request.Symbol);
            var price = new Price(request.Price);
            var quantity = new Quantity(request.Quantity);

            var account = await _accountRepository.GetByIdAsync(userId, cancellationToken);
            if (account is null)
                return Result<PlaceOrderResponseDto>.Failure("Account not found");

            // Reserve funds for buy orders up front.
            if (request.Side == OrderSide.Buy)
            {
                var notional = price.Value * quantity.Value;
                var money = new Money(notional, account.Balance.Currency);
                account.ReserveFunds(money);
                await _accountRepository.UpdateAsync(account, cancellationToken);
            }
            else if (request.Side == OrderSide.Sell)
            {
                var position = await _positionRepository.GetUserPositionForSymbolAsync(userId, request.Symbol, cancellationToken);
                if (position == null || position.AvailableQuantity.Value < request.Quantity)
                {
                    return Result<PlaceOrderResponseDto>.Failure("Insufficient position");
                }
                
                position.Reserve(quantity);
                await _positionRepository.UpdateAsync(position, cancellationToken);
            }

            var order = OrderDomain.Create(
                userId,
                symbolEntity.Id,
                price,
                quantity,
                request.Side);

            await _orderRepository.AddAsync(order, cancellationToken);

            var command = new AddOrderCommand
            {
                OrderId = order.Id,
                UserId = userId,
                Symbol = symbolValue,
                SymbolId = symbolEntity.Id,
                Price = request.Price,
                Quantity = request.Quantity,
                Side = request.Side,
                ReceivedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await _queue.EnqueueAsync(command, cancellationToken);

            return Result<PlaceOrderResponseDto>.Success(new PlaceOrderResponseDto
            {
                OrderId = order.Id,
                Status = OrderStatus.Open,
                Message = "Order queued for matching"
            });
        }
        catch (Exception ex)
        {
            return Result<PlaceOrderResponseDto>.Failure(
                $"Failed to place order: {ex.Message}");
        }
    }
}
