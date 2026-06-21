namespace TradingEngine.Infrastructure.Persistence.Outbox;

public sealed class SymbolCommandSequence
{
    private SymbolCommandSequence()
    {
    }

    public Guid SymbolId { get; private set; }
    public long LastSequenceId { get; private set; }

    public static SymbolCommandSequence Create(Guid symbolId)
    {
        return new SymbolCommandSequence
        {
            SymbolId = symbolId
        };
    }

    public long AllocateNext()
    {
        LastSequenceId++;
        return LastSequenceId;
    }
}
