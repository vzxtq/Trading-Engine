using System.Collections.Concurrent;
using TradingEngine.MatchingEngine.Commands;
using TradingEngine.MatchingEngine.Models;
using TradingEngine.Domain.ValueObjects;

namespace TradingEngine.MatchingEngine.Services;

public sealed class MatchingEngineProcessor : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, SymbolEngineProcessor> _engines = new();

    private long _fallbackSequenceId;

    public async ValueTask<ExecutionResult> ProcessAsync(MatchingEngineCommand command, long engineTimestamp)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Symbol);

        var sequenceId = command.SequenceId > 0
            ? command.SequenceId
            : Interlocked.Increment(ref _fallbackSequenceId);
        var engine = GetOrCreateEngine(command.Symbol);
        return await engine.EnqueueAsync(command, sequenceId, engineTimestamp);
    }

    public OrderBookSnapshot GetSnapshot(Symbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        var symbolKey = symbol.Value;
        if (!_engines.TryGetValue(symbolKey, out var engine))
            return new OrderBookSnapshot(symbolKey, [], []);

        return engine.Snapshot();
    }

    private SymbolEngineProcessor GetOrCreateEngine(Symbol symbol)
    {
        var symbolKey = symbol.Value;
        return _engines.GetOrAdd(symbolKey, key => new SymbolEngineProcessor(key));
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var engine in _engines.Values)
        {
            await engine.DisposeAsync();
        }
        _engines.Clear();
    }
}
