using FluentAssertions;
using TradingEngine.Domain.Enums;
using TradingEngine.Domain.ValueObjects;
using TradingEngine.MatchingEngine.Commands;
using TradingEngine.MatchingEngine.Services;
using TradingEngine.Infrastructure.Persistence.Outbox;

namespace TradingPlatform.IntegrationTests;

public sealed class DurableMatchingEngineTests
{
    [Fact]
    public async Task ProcessAsync_ShouldPreserveDurableSequenceId()
    {
        await using var processor = new MatchingEngineProcessor();
        var command = CreateLimitOrder(sequenceId: 42);

        var result = await processor.ProcessAsync(command);

        result.SequenceId.Should().Be(42);
    }

    [Fact]
    public async Task ReplayingCompletedCommands_ShouldRestoreOrderBook()
    {
        await using var original = new MatchingEngineProcessor();
        await using var recovered = new MatchingEngineProcessor();

        var first = CreateLimitOrder(sequenceId: 10, price: 150_0000);
        var second = CreateLimitOrder(sequenceId: 20, price: 149_0000);

        await original.ProcessAsync(first);
        await original.ProcessAsync(second);

        await recovered.ProcessAsync(first);
        await recovered.ProcessAsync(second);

        recovered.GetSnapshot(first.Symbol)
            .Should()
            .BeEquivalentTo(original.GetSnapshot(first.Symbol));
    }

    [Fact]
    public async Task ProcessAsync_ShouldAllocateFallbackSequencePerSymbol()
    {
        await using var processor = new MatchingEngineProcessor();

        var firstAapl = await processor.ProcessAsync(CreateLimitOrder(sequenceId: 0, symbol: "AAPL", receivedAt: 1));
        var firstMsft = await processor.ProcessAsync(CreateLimitOrder(sequenceId: 0, symbol: "MSFT", receivedAt: 2));
        var secondAapl = await processor.ProcessAsync(CreateLimitOrder(sequenceId: 0, symbol: "AAPL", receivedAt: 3, price: 151_0000));

        firstAapl.SequenceId.Should().Be(1);
        firstMsft.SequenceId.Should().Be(1);
        secondAapl.SequenceId.Should().Be(2);
    }

    [Fact]
    public void SymbolSequences_ShouldAdvanceIndependently()
    {
        var aapl = SymbolCommandSequence.Create(Guid.NewGuid());
        var msft = SymbolCommandSequence.Create(Guid.NewGuid());

        aapl.AllocateNext().Should().Be(1);
        aapl.AllocateNext().Should().Be(2);
        msft.AllocateNext().Should().Be(1);
    }

    private static AddOrderCommand CreateLimitOrder(
        long sequenceId,
        long price = 150_0000,
        string symbol = "AAPL",
        long? receivedAt = null)
    {
        return new AddOrderCommand
        {
            CommandOutboxId = Guid.NewGuid(),
            SequenceId = sequenceId,
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Symbol = new Symbol(symbol),
            SymbolId = GetSymbolId(symbol),
            Price = price,
            Quantity = 10_0000,
            Side = OrderSide.Sell,
            Type = OrderType.Limit,
            MaxTotalCost = 0,
            ReceivedAt = receivedAt ?? sequenceId
        };
    }

    private static Guid GetSymbolId(string symbol)
    {
        return symbol switch
        {
            "AAPL" => Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "MSFT" => Guid.Parse("22222222-2222-2222-2222-222222222222"),
            _ => Guid.NewGuid()
        };
    }
}
