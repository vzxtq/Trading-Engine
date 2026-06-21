using FluentAssertions;
using TradingEngine.Application.Features.Orders.Placement;
using TradingEngine.Application.Features.Orders.Placement.Strategies;
using TradingEngine.Application.Interfaces.Accounts;
using TradingEngine.Application.Interfaces.OrderPlacement;
using TradingEngine.Application.Interfaces.Positions;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.Enums;
using TradingEngine.Domain.ValueObjects;
using TradingEngine.MatchingEngine.Interfaces;
using TradingEngine.MatchingEngine.Models;
using TradingEngine.MatchingEngine.Scaling;

namespace TradingPlatform.IntegrationTests;

public sealed class OrderPlacementStrategyTests
{
    [Fact]
    public async Task LimitBuyStrategy_ShouldReserveExactOrderNotional()
    {
        var account = CreateAccount();
        var repository = new AccountRepositoryStub(account);
        var strategy = new LimitBuyOrderPlacementStrategy(repository);
        var context = CreateContext(account, price: 125.50m, quantity: 2m);

        var result = await strategy.ExecuteAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReservedAmount.Should().Be(251m);
        result.Value.EnginePrice.Should().Be(125.50m.ToEnginePrice());
        result.Value.EngineMaxTotalCost.Should().Be(251m.ToEngineNotional());
        account.ReservedBalance.Amount.Should().Be(251m);
        repository.UpdatedAccount.Should().BeSameAs(account);
    }

    [Fact]
    public async Task MarketBuyStrategy_ShouldReserveEstimatedCostWithSlippage()
    {
        var account = CreateAccount();
        var repository = new AccountRepositoryStub(account);
        var snapshotProvider = new SnapshotProviderStub(
            new OrderBookSnapshot(
                "AAPL",
                [],
                [
                    new PriceLevelSnapshot(
                        100m.ToEnginePrice(),
                        2m.ToEngineQuantity(),
                        [])
                ]));
        var strategy = new MarketBuyOrderPlacementStrategy(
            repository,
            snapshotProvider);
        var context = CreateContext(account, price: null, quantity: 2m);

        var result = await strategy.ExecuteAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReservedAmount.Should().Be(210m);
        result.Value.EnginePrice.Should().Be(0);
        result.Value.EngineMaxTotalCost.Should().Be(210m.ToEngineNotional());
        account.ReservedBalance.Amount.Should().Be(210m);
    }

    [Theory]
    [InlineData(OrderType.Limit, 10000)]
    [InlineData(OrderType.Market, 0)]
    public async Task SellStrategies_ShouldReservePositionQuantity(
        OrderType orderType,
        long expectedEnginePrice)
    {
        var account = CreateAccount();
        var position = PositionDomain.Create(
            account.Id,
            new Symbol("AAPL"),
            new Quantity(5m),
            90m)!;
        var repository = new PositionRepositoryStub(position);
        IOrderPlacementStrategy strategy = orderType == OrderType.Limit
            ? new LimitSellOrderPlacementStrategy(repository)
            : new MarketSellOrderPlacementStrategy(repository);
        var context = CreateContext(
            account,
            orderType == OrderType.Limit ? 100m : null,
            quantity: 2m);

        var result = await strategy.ExecuteAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReservedAmount.Should().Be(2m);
        result.Value.EnginePrice.Should().Be(expectedEnginePrice);
        result.Value.EngineMaxTotalCost.Should().Be(0);
        position.ReservedQuantity.Value.Should().Be(2m);
        repository.UpdatedPosition.Should().BeSameAs(position);
    }

    [Fact]
    public void Selector_ShouldSelectStrategyBySideAndType()
    {
        var accountRepository = new AccountRepositoryStub(CreateAccount());
        var positionRepository = new PositionRepositoryStub(null);
        var strategies = new IOrderPlacementStrategy[]
        {
            new LimitBuyOrderPlacementStrategy(accountRepository),
            new MarketBuyOrderPlacementStrategy(
                accountRepository,
                new SnapshotProviderStub(
                    new OrderBookSnapshot("AAPL", [], []))),
            new LimitSellOrderPlacementStrategy(positionRepository),
            new MarketSellOrderPlacementStrategy(positionRepository)
        };
        var selector = new OrderPlacementStrategySelector(strategies);

        selector.Select(OrderSide.Buy, OrderType.Limit)
            .Should().BeOfType<LimitBuyOrderPlacementStrategy>();
        selector.Select(OrderSide.Buy, OrderType.Market)
            .Should().BeOfType<MarketBuyOrderPlacementStrategy>();
        selector.Select(OrderSide.Sell, OrderType.Limit)
            .Should().BeOfType<LimitSellOrderPlacementStrategy>();
        selector.Select(OrderSide.Sell, OrderType.Market)
            .Should().BeOfType<MarketSellOrderPlacementStrategy>();
    }

    private static UserAccountDomain CreateAccount()
    {
        return UserAccountDomain.Create(
            "placement@test.com",
            "Order",
            "Placement",
            new Money(10_000m, Currency.USD));
    }

    private static OrderPlacementContext CreateContext(
        UserAccountDomain account,
        decimal? price,
        decimal quantity)
    {
        return new OrderPlacementContext(
            account.Id,
            account,
            new Symbol("AAPL"),
            price.HasValue ? new Price(price.Value) : null,
            new Quantity(quantity));
    }

    private sealed class AccountRepositoryStub : IAccountRepository
    {
        private readonly UserAccountDomain _account;

        public AccountRepositoryStub(UserAccountDomain account)
        {
            _account = account;
        }

        public UserAccountDomain? UpdatedAccount { get; private set; }

        public Task<UserAccountDomain?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<UserAccountDomain?>(
                id == _account.Id ? _account : null);
        }

        public Task AddAsync(
            UserAccountDomain account,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            UserAccountDomain account,
            CancellationToken cancellationToken)
        {
            UpdatedAccount = account;
            return Task.CompletedTask;
        }
    }

    private sealed class PositionRepositoryStub : IPositionRepository
    {
        private readonly PositionDomain? _position;

        public PositionRepositoryStub(PositionDomain? position)
        {
            _position = position;
        }

        public PositionDomain? UpdatedPosition { get; private set; }

        public Task<PositionDomain?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                _position?.Id == id ? _position : null);
        }

        public Task<PositionDomain?> GetUserPositionForSymbolAsync(
            Guid userId,
            string symbol,
            CancellationToken cancellationToken)
        {
            var matches = _position is not null
                          && _position.UserId == userId
                          && _position.SymbolValue.Value == symbol;

            return Task.FromResult(matches ? _position : null);
        }

        public Task UpdateAsync(
            PositionDomain position,
            CancellationToken cancellationToken)
        {
            UpdatedPosition = position;
            return Task.CompletedTask;
        }

        public Task AddAsync(
            PositionDomain position,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class SnapshotProviderStub : IOrderBookSnapshotProvider
    {
        private readonly OrderBookSnapshot _snapshot;

        public SnapshotProviderStub(OrderBookSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public Task<OrderBookSnapshot> GetSnapshotAsync(
            Symbol symbol,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_snapshot);
        }
    }
}
