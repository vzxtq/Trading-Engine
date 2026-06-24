using TradingEngine.Domain.Enums;
using TradingEngine.MatchingEngine.Commands;
using TradingEngine.MatchingEngine.Models;
using TradingEngine.MatchingEngine.Models.Notifications;

namespace TradingEngine.MatchingEngine.Services;

public sealed class SymbolEngine
{
    private readonly OrderBook _orderBook;

    public string Symbol => _orderBook.Symbol;

    public SymbolEngine(string symbol)
    {
        _orderBook = new OrderBook(symbol);
    }

    public ExecutionResult Process(MatchingEngineCommand command, long sequenceId)
    {
        return command switch
        {
            AddOrderCommand cmd => ProcessAddOrder(cmd, sequenceId),
            CancelOrderCommand cmd => ProcessCancelOrder(cmd, sequenceId),
            _ => new ExecutionResult.Rejected
            {
                Symbol = command.Symbol,
                SymbolId = command.SymbolId,
                SequenceId = sequenceId,
                EngineTimestamp = command.ReceivedAt,
                Reason = "Unknown command"
            }
        };
    }

    private ExecutionResult.Accepted ProcessAddOrder(AddOrderCommand command, long sequenceId)
    {
        var taker = new EngineOrder(
            command.OrderId,
            command.UserId,
            command.Symbol,
            command.SymbolId,
            command.Price,
            command.Quantity,
            command.Side,
            command.Type,
            command.MaxTotalCost,
            command.ReceivedAt);

        var trades = new List<ExecutedTrade>();
        var stateChanges = new List<OrderStateChange>();
        var selfTradePreventions = new List<SelfTradePreventionEvent>();

        var takerCancelledBySelfTradePrevention = MatchOrder(
            taker,
            command.ReceivedAt,
            trades,
            stateChanges,
            selfTradePreventions);

        var takerStatus = GetTakerStatus(
            taker,
            takerCancelledBySelfTradePrevention,
            trades.Count > 0);

        stateChanges.Add(new OrderStateChange(
            OrderId: taker.Id,
            UserId: taker.UserId,
            FilledQuantity: taker.FilledQuantity,
            RemainingQuantity: taker.RemainingQuantity,
            Status: takerStatus));

        if (!takerCancelledBySelfTradePrevention && !taker.IsFullyMatched && taker.Type == OrderType.Limit)
            _orderBook.AddOrder(taker);

        var orderBookChanges = ComputeOrderBookChanges(trades, taker);

        return new ExecutionResult.Accepted
        {
            Symbol = command.Symbol,
            SymbolId = command.SymbolId,
            SequenceId = sequenceId,
            EngineTimestamp = command.ReceivedAt,
            Trades = trades,
            StateChanges = stateChanges,
            OrderBookChanges = orderBookChanges,
            SelfTradePreventions = selfTradePreventions
        };
    }

    private ExecutionResult ProcessCancelOrder(CancelOrderCommand command, long sequenceId)
    {
        var order = _orderBook.FindOrder(command.OrderId);
        if (order is null)
            return new ExecutionResult.Rejected
            {
                Symbol = command.Symbol,
                SymbolId = command.SymbolId,
                SequenceId = sequenceId,
                EngineTimestamp = command.ReceivedAt,
                Reason = "Order not found"
            };

        _orderBook.RemoveOrder(command.OrderId);

        var isBuy = order.Side == OrderSide.Buy;
        var remaining = isBuy
            ? _orderBook.GetBidOrders().Where(o => o.Price == order.Price).Sum(o => o.RemainingQuantity)
            : _orderBook.GetAskOrders().Where(o => o.Price == order.Price).Sum(o => o.RemainingQuantity);

        var orderBookChanges = new List<OrderBookStateChangeDto>
        {
            new(order.Price, remaining, isBuy)
        };

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
            EngineTimestamp = command.ReceivedAt,
            Trades = [],
            StateChanges = [stateChange],
            OrderBookChanges = orderBookChanges
        };
    }

    private static OrderStatus GetTakerStatus(
        EngineOrder taker,
        bool cancelledBySelfTradePrevention,
        bool hasTrades)
    {
        if (cancelledBySelfTradePrevention)
        {
            if (taker.FilledQuantity > 0)
                return OrderStatus.PartiallyFilledCancelled;

            return OrderStatus.Cancelled;
        }

        if (taker.IsFullyMatched)
            return OrderStatus.Filled;

        if (taker.Type == OrderType.Market)
        {
            if (taker.FilledQuantity > 0)
                return OrderStatus.PartiallyFilledCancelled;

            return OrderStatus.Cancelled;
        }

        if (hasTrades)
            return OrderStatus.PartiallyFilled;

        return OrderStatus.Open;
    }

    private bool MatchOrder(
        EngineOrder taker,
        long engineTimestamp,
        List<ExecutedTrade> trades,
        List<OrderStateChange> stateChanges,
        List<SelfTradePreventionEvent> selfTradePreventions)
    {
        long takerCostSoFar = 0;
        var makers = taker.Side == OrderSide.Buy
            ? _orderBook.GetAskOrders()
            : _orderBook.GetBidOrders();

        foreach (var maker in makers)
        {
            if (taker.IsFullyMatched)
                break;

            if (!IsPriceCompatible(taker, maker))
                break;

            if (maker.UserId == taker.UserId)
            {
                selfTradePreventions.Add(new SelfTradePreventionEvent(
                    taker.Id,
                    maker.Id,
                    taker.UserId,
                    SelfTradePreventionPolicy.CancelTaker,
                    "Self-trade prevention: cancel-taker because maker and taker belong to the same user."));

                return true;
            }

            var quantity = Math.Min(taker.RemainingQuantity, maker.RemainingQuantity);

            if (taker.Side == OrderSide.Buy && taker.MaxTotalCost > 0)
            {
                var maxAffordableQuantity = (taker.MaxTotalCost - takerCostSoFar) / maker.Price;
                if (maxAffordableQuantity <= 0)
                    break; // Exhausted reserved funds

                quantity = Math.Min(quantity, maxAffordableQuantity);
            }

            taker.Fill(quantity);
            maker.Fill(quantity);

            if (taker.Side == OrderSide.Buy)
            {
                //checked to prevent silent overflow which could lead to accepting orders that exceed the max total cost
                takerCostSoFar = checked(takerCostSoFar + (quantity * maker.Price));
            }

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

        return false;
    }

    private static bool IsPriceCompatible(EngineOrder taker, EngineOrder maker)
    {
        if (taker.Type == OrderType.Market)
            return true;

        return taker.Side == OrderSide.Buy
            ? taker.Price >= maker.Price
            : taker.Price <= maker.Price;
    }

    private List<OrderBookStateChangeDto> ComputeOrderBookChanges(List<ExecutedTrade> trades, EngineOrder taker)
    {
        var changes = new List<OrderBookStateChangeDto>();
        var updatedPrices = new HashSet<long>();

        // For maker side changes from trades
        var makerIsBuy = taker.Side == OrderSide.Sell;
        var makerOrders = makerIsBuy
            ? _orderBook.GetBidOrders()
            : _orderBook.GetAskOrders();

        var remainingByPrice = makerOrders
            .GroupBy(o => o.Price)
            .ToDictionary(g => g.Key, g => g.Sum(o => o.RemainingQuantity));

        foreach (var trade in trades)
        {
            if (updatedPrices.Add(trade.Price))
            {
                remainingByPrice.TryGetValue(trade.Price, out var remaining);
                changes.Add(new OrderBookStateChangeDto(trade.Price, remaining, makerIsBuy));
            }
        }

        // For taker side changes (limit orders that are partially or entirely unmatched)
        if (taker.Type == OrderType.Limit && taker.RemainingQuantity > 0)
        {
            var takerOrders = taker.Side == OrderSide.Buy
                ? _orderBook.GetBidOrders()
                : _orderBook.GetAskOrders();

            var remaining = takerOrders
                .Where(o => o.Price == taker.Price)
                .Sum(o => o.RemainingQuantity);

            changes.Add(new OrderBookStateChangeDto(taker.Price, remaining, taker.Side == OrderSide.Buy));
        }

        return changes;
    }

    public OrderBookSnapshot Snapshot() => _orderBook.Snapshot();
}