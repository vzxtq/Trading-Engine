using TradingEngine.MatchingEngine.Models;

namespace TradingEngine.MatchingEngine.Interfaces
{
    public interface IExecutionResultDispatcher
    {
        Task DispatchAsync(ExecutionResult result, CancellationToken cancellationToken);
    }
}
