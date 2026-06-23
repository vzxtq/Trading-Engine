using TradingEngine.Domain.Enums;
using TradingEngine.Domain.Common;
using TradingEngine.Domain.Events;
using TradingEngine.Domain.Events.Orders;
using TradingEngine.Domain.ValueObjects;

namespace TradingEngine.Domain.Entities
{
    public class OrderDomain : AggregateRoot
    {
        public Guid UserId { get; private set; }
        public Guid SymbolId { get; private set; }

        public virtual SymbolDomain Symbol { get; } = null!;
        public Price? Price { get; private set; }

        public Quantity Quantity { get; private set; } = null!;
        public Quantity RemainingQuantity { get; private set; } = null!;
        public decimal FilledQuantity => Quantity.Value - RemainingQuantity.Value;

        public OrderSide Side { get; private set; }
        public OrderType Type { get; private set; }
        public OrderStatus Status { get; private set; }
        
        public string? IdempotencyKey { get; private set; }

        /// <summary>
        /// The actual amount reserved at placement time (funds for Buy, quantity for Sell).
        /// Used for correct release on cancellation.
        /// </summary>
        public decimal ReservedAmount { get; private set; }

        private OrderDomain() { }

        public static OrderDomain Create(
            Guid userId,
            Guid symbolId,
            Price? price,
            Quantity quantity,
            OrderSide side,
            OrderType type,
            decimal reservedAmount,
            string? idempotencyKey = null)
        {
            var order = new OrderDomain
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SymbolId = symbolId,
                Price = price,
                Quantity = quantity,
                RemainingQuantity = quantity,
                Side = side,
                Type = type,
                Status = OrderStatus.Open,
                ReservedAmount = reservedAmount,
                IdempotencyKey = idempotencyKey
            };

            order.RaiseDomainEvent(
                new OrderPlacedEvent(order.Id, userId, symbolId, price, quantity, side));

            return order;
        }

        public void Fill(Quantity filledQuantity)
        {
            if (filledQuantity.IsGreaterThan(RemainingQuantity))
                throw new InvalidOperationException("Fill exceeds remaining quantity");

            RemainingQuantity = RemainingQuantity.Subtract(filledQuantity);

            if (RemainingQuantity.Value == 0)
            {
                Status = OrderStatus.Filled;

                RaiseDomainEvent(
                    new OrderFilledEvent(
                        Id,
                        UserId,
                        SymbolId,
                        Quantity));
            }
            else
            {
                Status = OrderStatus.PartiallyFilled;
            }

            UpdatedAt = DateTime.UtcNow;
        }

        public void Cancel()
        {
            if (Status == OrderStatus.Filled ||
                Status == OrderStatus.Cancelled ||
                Status == OrderStatus.Rejected)
            {
                throw new InvalidOperationException(
                    $"Cannot cancel order with status {Status}");
            }

            Status = OrderStatus.Cancelled;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new OrderCancelledEvent(Id));
        }

        public void ApplyStateChange(decimal reportedFilledQuantity, OrderStatus targetStatus)
        {
            var alreadyFilled = Quantity.Value - RemainingQuantity.Value;
            var newlyFilled = reportedFilledQuantity - alreadyFilled;

            if (newlyFilled > 0)
                Fill(new Quantity(newlyFilled));

            if (Status == targetStatus) return;

            if (targetStatus == OrderStatus.Cancelled)
                Cancel();
            else if (targetStatus == OrderStatus.Filled && RemainingQuantity.Value == 0)
                Fill(new Quantity(0));
        }
    }
}
