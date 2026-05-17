using TradingEngine.MatchingEngine.Commands;

namespace TradingEngine.MatchingEngine.Interfaces
{
    public interface IMatchingEngineQueue
    {
        ValueTask EnqueueAsync(MatchingEngineCommand command, CancellationToken cancellationToken);
    }
}
