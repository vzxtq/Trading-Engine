namespace TradingEngine.Infrastructure.Persistence.Outbox;

public sealed class ProcessedExecutionReceipt
{
    private ProcessedExecutionReceipt()
    {
    }

    public Guid Id { get; private set; }
    public Guid SymbolId { get; private set; }
    public long SequenceId { get; private set; }
    public DateTime ProcessedAt { get; private set; }

    public static ProcessedExecutionReceipt Create(Guid symbolId, long sequenceId)
    {
        return new ProcessedExecutionReceipt
        {
            Id = Guid.NewGuid(),
            SymbolId = symbolId,
            SequenceId = sequenceId,
            ProcessedAt = DateTime.UtcNow
        };
    }
}
