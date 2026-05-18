using TradingEngine.Domain.Enums;
using TradingEngine.MatchingEngine.Commands;
using TradingEngine.MatchingEngine.Models;

namespace TradingEngine.MatchingEngine.Services;

public sealed class SymbolEngine
{
    private readonly OrderBook _orderBook;

    public string Symbol => _orderBook.Symbol;

    public SymbolEngine(string symbol)
    {
        _orderBook = new OrderBook(symbol);
    }

    public ExecutionResult Process(MatchingEngineCommand command, long sequenceId, long engineTimestamp)
    {
        return command switch
        {
            AddOrderCommand cmd => ProcessAddOrder(cmd, sequenceId, engineTimestamp),
            CancelOrderCommand cmd => ProcessCancelOrder(cmd, sequenceId, engineTimestamp),
            _ => new ExecutionResult.Rejected
            {
                Symbol = command.Symbol,
                SymbolId = command.SymbolId,
                SequenceId = sequenceId,
                EngineTimestamp = engineTimestamp,
                Reason = "Unknown command"
            }
        };
    }

    private ExecutionResult ProcessAddOrder(AddOrderCommand command, long sequenceId, long engineTimestamp)
    {
        var taker = new EngineOrder(
            command.OrderId,
            command.UserId,
            command.Symbol,
            command.SymbolId,
            command.Price,
            command.Quantity,
            command.Side,
            command.ReceivedAt);

        var trades = new List<ExecutedTrade>();
        var stateChanges = new List<OrderStateChange>();

        MatchOrder(taker, engineTimestamp, trades, stateChanges);

        var takerStatus = taker.IsFullyMatched
            ? OrderStatus.Filled
            : trades.Count > 0
                ? OrderStatus.PartiallyFilled
                : OrderStatus.Open;

        stateChanges.Add(new OrderStateChange(
            OrderId: taker.Id,
            UserId: taker.UserId,
            FilledQuantity: taker.FilledQuantity,
            RemainingQuantity: taker.RemainingQuantity,
            Status: takerStatus));

        if (!taker.IsFullyMatched)
            _orderBook.AddOrder(taker);

        return new ExecutionResult.Accepted
        {
            Symbol = command.Symbol,
            SymbolId = command.SymbolId,
            SequenceId = sequenceId,
            EngineTimestamp = engineTimestamp,
            Trades = trades,
            StateChanges = stateChanges
        };
    }

    private ExecutionResult ProcessCancelOrder(CancelOrderCommand command, long sequenceId, long engineTimestamp)
    {
        var order = _orderBook.FindOrder(command.OrderId);
        if (order is null)
            return new ExecutionResult.Rejected
            {
                Symbol = command.Symbol,
                SymbolId = command.SymbolId,
                SequenceId = sequenceId,
                EngineTimestamp = engineTimestamp,
                Reason = "Order not found"
            };

        _orderBook.RemoveOrder(command.OrderId);

        var stateChange = new OrderStateChange(
            command.OrderId,
            order.UserId,
            order.FilledQuantity,
            order.RemainingQuantity,
            OrderStatus.Cancelled);

        return new ExecutionResult.Accepted
        {
            Symbol = command.Symbol,
            SymbolId = command.SymbolId,
            SequenceId = sequenceId,
            EngineTimestamp = engineTimestamp,
            Trades = [],
            StateChanges = [stateChange]
        };
    }

    private void MatchOrder(
        EngineOrder taker,
        long engineTimestamp,
        List<ExecutedTrade> trades,
        List<OrderStateChange> stateChanges)
    {
        var makers = taker.Side == OrderSide.Buy
            ? _orderBook.GetAskOrders()
            : _orderBook.GetBidOrders();

        foreach (var maker in makers)
        {
            if (taker.IsFullyMatched)
                break;

            if (!IsPriceCompatible(taker, maker))
                break;

            var quantity = Math.Min(taker.RemainingQuantity, maker.RemainingQuantity);

            taker.Fill(quantity);
            maker.Fill(quantity);

            trades.Add(new ExecutedTrade(
                TradeId: Guid.NewGuid(),
                BuyOrderId: taker.Side == OrderSide.Buy ? taker.Id : maker.Id,
                SellOrderId: taker.Side == OrderSide.Sell ? taker.Id : maker.Id,
                BuyerId: taker.Side == OrderSide.Buy ? taker.UserId : maker.UserId,
                SellerId: taker.Side == OrderSide.Sell ? taker.UserId : maker.UserId,
                SymbolId: taker.SymbolId,
                Price: maker.Price,
                Quantity: quantity,
                ExecutedAt: engineTimestamp));

            var makerStatus = maker.IsFullyMatched ? OrderStatus.Filled : OrderStatus.PartiallyFilled;

            stateChanges.Add(new OrderStateChange(
                OrderId: maker.Id,
                UserId: maker.UserId,
                FilledQuantity: maker.FilledQuantity,
                RemainingQuantity: maker.RemainingQuantity,
                Status: makerStatus));

            if (maker.IsFullyMatched)
                _orderBook.RemoveOrder(maker.Id);
        }
    }

    private static bool IsPriceCompatible(EngineOrder taker, EngineOrder maker)
    {
        return taker.Side == OrderSide.Buy
            ? taker.Price >= maker.Price
            : taker.Price <= maker.Price;
    }

    public OrderBookSnapshot Snapshot() => _orderBook.Snapshot();
}
