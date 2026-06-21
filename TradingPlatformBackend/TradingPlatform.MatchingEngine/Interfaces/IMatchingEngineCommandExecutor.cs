using TradingEngine.MatchingEngine.Commands;
using TradingEngine.MatchingEngine.Models;

namespace TradingEngine.MatchingEngine.Interfaces;

public interface IMatchingEngineCommandExecutor
{
    Task<ExecutionResult> ExecuteAsync(
        MatchingEngineCommand command,
        CancellationToken cancellationToken);
}
