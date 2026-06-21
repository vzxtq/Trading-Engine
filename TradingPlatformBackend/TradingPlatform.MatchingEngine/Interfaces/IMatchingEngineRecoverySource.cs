using TradingEngine.MatchingEngine.Commands;

namespace TradingEngine.MatchingEngine.Interfaces;

public interface IMatchingEngineRecoverySource
{
    Task<IReadOnlyList<MatchingEngineCommand>> LoadCompletedCommandsAsync(
        CancellationToken cancellationToken);
}
