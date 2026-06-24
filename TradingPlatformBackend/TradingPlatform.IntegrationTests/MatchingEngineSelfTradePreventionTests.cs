using FluentAssertions;
using TradingEngine.Domain.Enums;
using TradingEngine.Domain.ValueObjects;
using TradingEngine.MatchingEngine.Commands;
using TradingEngine.MatchingEngine.Models;
using TradingEngine.MatchingEngine.Services;

namespace TradingPlatform.IntegrationTests;

public sealed class MatchingEngineSelfTradePreventionTests
{
    private static readonly Symbol TestSymbol = new("AAPL");
    private static readonly Guid TestSymbolId = Guid.NewGuid();

    [Fact]
    public void SelfTradePrevention_ShouldCancelTakerAndLeaveMakerResting()
    {
        var engine = new SymbolEngine(TestSymbol.Value);
        var userId = Guid.NewGuid();
        var makerOrderId = Guid.NewGuid();
        var takerOrderId = Guid.NewGuid();

        engine.Process(CreateLimitOrder(
            makerOrderId,
            userId,
            OrderSide.Sell,
            price: 150_0000,
            quantity: 10_0000), sequenceId: 1);

        var result = (ExecutionResult.Accepted)engine.Process(CreateLimitOrder(
            takerOrderId,
            userId,
            OrderSide.Buy,
            price: 150_0000,
            quantity: 10_0000), sequenceId: 2);

        result.Trades.Should().BeEmpty();
        result.SelfTradePreventions.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new SelfTradePreventionEvent(
                takerOrderId,
                makerOrderId,
                userId,
                SelfTradePreventionPolicy.CancelTaker,
                "Self-trade prevention: cancel-taker because maker and taker belong to the same user."));

        result.StateChanges.Should().ContainSingle(x => x.OrderId == takerOrderId)
            .Which.Status.Should().Be(OrderStatus.Cancelled);

        var snapshot = engine.Snapshot();
        snapshot.Asks.Should().ContainSingle()
            .Which.Orders.Should().ContainSingle(x => x.OrderId == makerOrderId);
        snapshot.Bids.Should().BeEmpty();
    }

    [Fact]
    public void SelfTradePrevention_AfterPartialFill_ShouldCancelRemainingTakerQuantity()
    {
        var engine = new SymbolEngine(TestSymbol.Value);
        var takerUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var otherMakerOrderId = Guid.NewGuid();
        var selfMakerOrderId = Guid.NewGuid();
        var takerOrderId = Guid.NewGuid();

        engine.Process(CreateLimitOrder(
            otherMakerOrderId,
            otherUserId,
            OrderSide.Sell,
            price: 149_0000,
            quantity: 4_0000), sequenceId: 1);
        engine.Process(CreateLimitOrder(
            selfMakerOrderId,
            takerUserId,
            OrderSide.Sell,
            price: 150_0000,
            quantity: 10_0000), sequenceId: 2);

        var result = (ExecutionResult.Accepted)engine.Process(CreateLimitOrder(
            takerOrderId,
            takerUserId,
            OrderSide.Buy,
            price: 150_0000,
            quantity: 10_0000), sequenceId: 3);

        result.Trades.Should().ContainSingle();
        result.SelfTradePreventions.Should().ContainSingle()
            .Which.Policy.Should().Be(SelfTradePreventionPolicy.CancelTaker);

        result.StateChanges.Should().ContainSingle(x => x.OrderId == takerOrderId)
            .Which.Should().BeEquivalentTo(new OrderStateChange(
                takerOrderId,
                takerUserId,
                FilledQuantity: 4_0000,
                RemainingQuantity: 6_0000,
                Status: OrderStatus.PartiallyFilledCancelled));

        var snapshot = engine.Snapshot();
        snapshot.Asks.SelectMany(x => x.Orders).Should().ContainSingle(x => x.OrderId == selfMakerOrderId);
        snapshot.Bids.Should().BeEmpty();
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
}