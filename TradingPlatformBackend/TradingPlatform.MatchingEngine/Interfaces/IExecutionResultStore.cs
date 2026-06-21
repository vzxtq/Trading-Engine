using TradingEngine.MatchingEngine.Models;

namespace TradingEngine.MatchingEngine.Interfaces;

public interface IExecutionResultStore
{
    Task WriteAsync(
        Guid commandOutboxId,
        ExecutionResult result,
        CancellationToken cancellationToken);
}
