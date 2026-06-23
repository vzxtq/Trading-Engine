namespace TradingEngine.Infrastructure.Persistence.Outbox;

public sealed class OrderCommandOutboxEntry
{
    private OrderCommandOutboxEntry()
    {
    }

    public Guid Id { get; private set; }
    public long EnqueueId { get; private set; }
    public long? SequenceId { get; private set; }
    public Guid SymbolId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid? ActiveCancellationOrderId { get; private set; }
    public OrderCommandType CommandType { get; private set; }
    public string Payload { get; private set; } = string.Empty;
    public OrderCommandStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? DispatchedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public static OrderCommandOutboxEntry Create(
        Guid symbolId,
        Guid orderId,
        OrderCommandType commandType,
        string payload)
    {
        return new OrderCommandOutboxEntry
        {
            Id = Guid.NewGuid(),
            SymbolId = symbolId,
            OrderId = orderId,
            ActiveCancellationOrderId = commandType == OrderCommandType.CancelOrder
                ? orderId
                : null,
            CommandType = commandType,
            Payload = payload,
            Status = OrderCommandStatus.Pending
        };
    }

    public void MarkDispatched()
    {
        Status = OrderCommandStatus.Dispatched;
        DispatchedAt = DateTime.UtcNow;
        AttemptCount++;
        LastError = null;
    }

    public void AssignSequence(long sequenceId)
    {
        if (SequenceId is not null)
            return;

        SequenceId = sequenceId;
    }

    public void MarkCompleted()
    {
        Status = OrderCommandStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        LastError = null;
    }

    public void MarkSettled()
    {
        ActiveCancellationOrderId = null;
    }

    public void MarkPending(string? error = null)
    {
        Status = OrderCommandStatus.Pending;
        DispatchedAt = null;
        LastError = error;
    }
}

public enum OrderCommandType
{
    AddOrder,
    CancelOrder
}

public enum OrderCommandStatus
{
    Pending,
    Dispatched,
    Completed,
    Failed
}
