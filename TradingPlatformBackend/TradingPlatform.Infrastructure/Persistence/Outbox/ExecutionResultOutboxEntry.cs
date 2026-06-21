namespace TradingEngine.Infrastructure.Persistence.Outbox;

public sealed class ExecutionResultOutboxEntry
{
    private ExecutionResultOutboxEntry()
    {
    }

    public Guid Id { get; private set; }
    public Guid CommandOutboxId { get; private set; }
    public Guid SymbolId { get; private set; }
    public long SequenceId { get; private set; }
    public string ResultType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public ExecutionResultOutboxStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    public static ExecutionResultOutboxEntry Create(
        Guid commandOutboxId,
        Guid symbolId,
        long sequenceId,
        string resultType,
        string payload)
    {
        return new ExecutionResultOutboxEntry
        {
            Id = Guid.NewGuid(),
            CommandOutboxId = commandOutboxId,
            SymbolId = symbolId,
            SequenceId = sequenceId,
            ResultType = resultType,
            Payload = payload,
            Status = ExecutionResultOutboxStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAttempt() => AttemptCount++;

    public void MarkProcessed()
    {
        Status = ExecutionResultOutboxStatus.Processed;
        ProcessedAt = DateTime.UtcNow;
        LastError = null;
    }

    public void RecordFailure(string error)
    {
        LastError = error;
        AttemptCount++;
    }
}

public enum ExecutionResultOutboxStatus
{
    Pending,
    Processed,
    Failed
}
