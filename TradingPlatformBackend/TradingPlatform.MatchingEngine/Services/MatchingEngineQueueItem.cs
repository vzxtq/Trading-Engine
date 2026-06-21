using TradingEngine.MatchingEngine.Commands;
using TradingEngine.MatchingEngine.Models;

namespace TradingEngine.MatchingEngine.Services;

internal sealed record MatchingEngineQueueItem(
    MatchingEngineCommand Command,
    TaskCompletionSource<ExecutionResult>? Completion = null);
