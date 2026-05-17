using Microsoft.Extensions.DependencyInjection;
using TradingEngine.MatchingEngine.Interfaces;
using TradingEngine.MatchingEngine.Models;

namespace TradingEngine.MatchingEngine.Services
{
    /// <summary>
    /// Resolves scoped handlers per dispatch to avoid lifetime mismatches (singleton host -> scoped handlers with DbContext).
    /// </summary>
    public sealed class ExecutionResultDispatcher : IExecutionResultDispatcher
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ExecutionResultDispatcher(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        public async Task DispatchAsync(ExecutionResult result, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(result);

            await using var scope = _scopeFactory.CreateAsyncScope();
            var handlers = scope.ServiceProvider.GetServices<IExecutionResultHandler>();

            foreach (var handler in handlers)
            {
                await handler.HandleAsync(result, cancellationToken);
            }
        }
    }
}
