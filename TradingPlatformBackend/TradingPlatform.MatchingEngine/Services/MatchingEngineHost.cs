using System.Linq;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingEngine.MatchingEngine.Interfaces;
using TradingEngine.MatchingEngine.Commands;
using TradingEngine.MatchingEngine.Models;
using TradingEngine.Domain.ValueObjects;

namespace TradingEngine.MatchingEngine.Services;

internal sealed record MatchingEngineShard(
    int Id,
    Channel<MatchingEngineCommand> Channel,
    MatchingEngineWorker Worker,
    MatchingEngineProcessor Processor)
{
    public ChannelWriter<MatchingEngineCommand> Writer => Channel.Writer;
}

/// <summary>
/// Orchestrates sharded, per-symbol workers. Symbols are deterministically mapped to shards by hash.
/// </summary>
public sealed class MatchingEngineHost : IMatchingEngineQueue, IOrderBookSnapshotProvider, IMatchingEngineRunner, IAsyncDisposable
{
    private readonly MatchingEngineShard[] _shards;
    private readonly MatchingEngineOptions _options;
    private Task[] _workerTasks = [];
    private readonly ILogger<MatchingEngineHost> _logger;

    public MatchingEngineHost(
        IOptions<MatchingEngineOptions> options,
        IExecutionResultDispatcher dispatcher,
        IEngineTimeProvider timeProvider,
        ILoggerFactory loggerFactory,
        ILogger<MatchingEngineHost> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        var shardCount = Math.Max(1, _options.ShardCount);

        _shards = new MatchingEngineShard[shardCount];

        for (var i = 0; i < shardCount; i++)
        {
            var channel = Channel.CreateBounded<MatchingEngineCommand>(new BoundedChannelOptions(_options.ChannelCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = _options.FullMode
            });

            var processor = new MatchingEngineProcessor();
            var workerLogger = loggerFactory.CreateLogger<MatchingEngineWorker>();
            var worker = new MatchingEngineWorker(processor, dispatcher, timeProvider, channel.Reader, workerLogger);

            _shards[i] = new MatchingEngineShard(i, channel, worker, processor);
        }

        _logger = logger;

    }

    public ValueTask EnqueueAsync(MatchingEngineCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return GetWriter(command.Symbol.Value).WriteAsync(command, cancellationToken);
    }

    public async Task<OrderBookSnapshot> GetSnapshotAsync(Symbol symbol, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        var tcs = new TaskCompletionSource<OrderBookSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cmd = new SnapshotOrderBookCommand { Symbol = symbol, Completion = tcs };

        await GetWriter(symbol.Value).WriteAsync(cmd, cancellationToken).ConfigureAwait(false);
        return await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task RunAsync(CancellationToken ct)
    {
        _workerTasks = _shards.Select(shard => shard.Worker.RunAsync(ct)).ToArray();
        return Task.WhenAll(_workerTasks);
    }

    private ChannelWriter<MatchingEngineCommand> GetWriter(string symbol)
    {
        var index = GetShardIndex(symbol);
        return _shards[index].Writer;
    }

    private int GetShardIndex(string symbol)
    {
        var hash = StringComparer.OrdinalIgnoreCase.GetHashCode(symbol);
        return (hash & 0x7FFFFFFF) % _shards.Length;
    }

    public async ValueTask DisposeAsync()
    {
        // 1. Complete all channel writers to signal workers to stop after draining
        foreach (var shard in _shards)
        {
            shard.Writer.TryComplete();
        }

        // 2. Wait for workers to finish draining the queue with a 5s timeout
        if (_workerTasks.Length > 0)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                await Task.WhenAll(_workerTasks).WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("MatchingEngineHost shutdown timed out after 5s. Some commands may not have been processed.");
            }
        }

        // 3. Dispose all processors
        foreach (var shard in _shards)
        {
            await shard.Processor.DisposeAsync();
        }
    }
}
