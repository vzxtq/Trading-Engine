using FluentAssertions;
using TradingEngine.Domain.Enums;
using TradingEngine.Domain.ValueObjects;
using TradingEngine.MatchingEngine.Commands;
using TradingEngine.MatchingEngine.Models;
using TradingEngine.MatchingEngine.Services;

namespace TradingPlatform.IntegrationTests;

public class MatchingEngineCancellationTests
{
    private static readonly Symbol TestSymbol = new("AAPL");
    private static readonly Guid TestSymbolId = Guid.NewGuid();

    [Fact]
    public void CancellationBeforeCross_ShouldRemoveOrderBeforeLaterExecution()
    {
        var engine = new SymbolEngine(TestSymbol.Value);
        var sellerOrderId = Guid.NewGuid();

        engine.Process(CreateLimitOrder(
            sellerOrderId,
            Guid.NewGuid(),
            OrderSide.Sell,
            price: 150_0000,
            quantity: 10_0000), sequenceId: 1, engineTimestamp: 1);

        var cancellation = engine.Process(
            CreateCancellation(sellerOrderId),
            sequenceId: 2,
            engineTimestamp: 2);

        var laterBuy = engine.Process(CreateLimitOrder(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrderSide.Buy,
            price: 150_0000,
            quantity: 10_0000), sequenceId: 3, engineTimestamp: 3);

        cancellation.Should().BeOfType<ExecutionResult.Accepted>();
        ((ExecutionResult.Accepted)laterBuy).Trades.Should().BeEmpty();
        engine.Snapshot().Asks.Should().BeEmpty();
    }

    [Fact]
    public void ExecutionBeforeCancellation_ShouldRejectLateCancellation()
    {
        var engine = new SymbolEngine(TestSymbol.Value);
        var sellerOrderId = Guid.NewGuid();

        engine.Process(CreateLimitOrder(
            sellerOrderId,
            Guid.NewGuid(),
            OrderSide.Sell,
            price: 150_0000,
            quantity: 10_0000), sequenceId: 1, engineTimestamp: 1);

        var execution = engine.Process(CreateLimitOrder(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrderSide.Buy,
            price: 150_0000,
            quantity: 10_0000), sequenceId: 2, engineTimestamp: 2);

        var lateCancellation = engine.Process(
            CreateCancellation(sellerOrderId),
            sequenceId: 3,
            engineTimestamp: 3);

        ((ExecutionResult.Accepted)execution).Trades.Should().ContainSingle();
        lateCancellation.Should().BeOfType<ExecutionResult.Rejected>()
            .Which.Reason.Should().Be("Order not found");
    }

    private static AddOrderCommand CreateLimitOrder(
        Guid orderId,
        Guid userId,
        OrderSide side,
        long price,
        long quantity)
    {
        return new AddOrderCommand
        {
            OrderId = orderId,
            UserId = userId,
            Symbol = TestSymbol,
            SymbolId = TestSymbolId,
            Price = price,
            Quantity = quantity,
            Side = side,
            Type = OrderType.Limit,
            MaxTotalCost = side == OrderSide.Buy ? price * quantity : 0,
            ReceivedAt = 1
        };
    }

    private static CancelOrderCommand CreateCancellation(Guid orderId)
    {
        return new CancelOrderCommand
        {
            OrderId = orderId,
            Symbol = TestSymbol,
            SymbolId = TestSymbolId
        };
    }
}
