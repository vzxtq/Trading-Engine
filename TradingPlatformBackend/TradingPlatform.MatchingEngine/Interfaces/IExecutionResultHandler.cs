using TradingEngine.MatchingEngine.Models;

namespace TradingEngine.MatchingEngine.Interfaces
{
    public interface IExecutionResultHandler
    {
        Task HandleAsync(ExecutionResult result, CancellationToken cancellationToken);
    }
}
