using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Channels;
using TradingEngine.Domain.ValueObjects;
using TradingEngine.MatchingEngine.Commands;
using TradingEngine.MatchingEngine.Interfaces;
using TradingEngine.MatchingEngine.Models;

namespace TradingEngine.MatchingEngine.Services;

internal sealed record MatchingEngineShard(
    int Id,
    Channel<MatchingEngineQueueItem> Channel,
    MatchingEngineWorker Worker,
    MatchingEngineProcessor Processor)
{
    public ChannelWriter<MatchingEngineQueueItem> Writer => Channel.Writer;
}

/// <summary>
/// Orchestrates sharded, per-symbol workers. Symbols are deterministically mapped to shards by hash.
/// </summary>
public sealed class MatchingEngineHost :
    IMatchingEngineQueue,
    IMatchingEngineCommandExecutor,
    IOrderBookSnapshotProvider,
    IMatchingEngineRunner,
    IMatchingEngineReadiness,
    IAsyncDisposable
{
    private readonly MatchingEngineShard[] _matchingEngineShards;
    private readonly MatchingEngineOptions _matchingEngineOptions;
    private Task[] _workerTasks = [];
    private readonly ILogger<MatchingEngineHost> _logger;
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public MatchingEngineHost(
        IOptions<MatchingEngineOptions> options,
        IExecutionResultDispatcher dispatcher,
        IExecutionResultStore resultStore,
        ILoggerFactory loggerFactory,
        ILogger<MatchingEngineHost> logger)
    {
        _matchingEngineOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        var shardCount = Math.Max(1, _matchingEngineOptions.ShardCount);

        _matchingEngineShards = new MatchingEngineShard[shardCount];

        for (var i = 0; i < shardCount; i++)
        {
            var channel = Channel.CreateBounded<MatchingEngineQueueItem>(new BoundedChannelOptions(_matchingEngineOptions.ChannelCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = _matchingEngineOptions.FullMode
            });

            var processor = new MatchingEngineProcessor();
            var workerLogger = loggerFactory.CreateLogger<MatchingEngineWorker>();
            var worker = new MatchingEngineWorker(
                processor,
                dispatcher,
                resultStore,
                channel.Reader,
                workerLogger);

            _matchingEngineShards[i] = new MatchingEngineShard(i, channel, worker, processor);
        }

        _logger = logger;

    }

    public ValueTask EnqueueAsync(MatchingEngineCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return GetWriter(command.Symbol.Value).WriteAsync(
            new MatchingEngineQueueItem(command),
            cancellationToken);
    }

    public async Task<ExecutionResult> ExecuteAsync(
        MatchingEngineCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var completion = new TaskCompletionSource<ExecutionResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        await GetWriter(command.Symbol.Value)
            .WriteAsync(new MatchingEngineQueueItem(command, completion), cancellationToken)
            .ConfigureAwait(false);

        return await completion.Task
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<OrderBookSnapshot> GetSnapshotAsync(Symbol symbol, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        var tcs = new TaskCompletionSource<OrderBookSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cmd = new SnapshotOrderBookCommand
        {
            Symbol = symbol,
            SymbolId = Guid.Empty,
            ReceivedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Completion = tcs
        };

        await GetWriter(symbol.Value)
            .WriteAsync(new MatchingEngineQueueItem(cmd), cancellationToken)
            .ConfigureAwait(false);
        return await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task RunAsync(CancellationToken ct)
    {
        _workerTasks = _matchingEngineShards.Select(shard => shard.Worker.RunAsync(ct)).ToArray();
        _ready.TrySetResult();
        return Task.WhenAll(_workerTasks);
    }

    public Task WaitUntilReadyAsync(CancellationToken cancellationToken)
    {
        return _ready.Task.WaitAsync(cancellationToken);
    }

    public async Task RecoverAsync(
        IReadOnlyList<MatchingEngineCommand> commands,
        CancellationToken ct)
    {
        foreach (var command in commands
                     .OrderBy(x => x.SequenceId))
        {
            ct.ThrowIfCancellationRequested();
            var shard = _matchingEngineShards[GetShardIndex(command.Symbol.Value)];

            await shard.Processor.ProcessAsync(command);
        }
    }

    #region Private Methods
    private ChannelWriter<MatchingEngineQueueItem> GetWriter(string symbol)
    {
        var index = GetShardIndex(symbol);
        return _matchingEngineShards[index].Writer;
    }

    private int GetShardIndex(string symbol)
    {
        var hash = StringComparer.OrdinalIgnoreCase.GetHashCode(symbol);
        return (hash & 0x7FFFFFFF) % _matchingEngineShards.Length;
    }

    public async ValueTask DisposeAsync()
    {
        // 1. Complete all channel writers to signal workers to stop after draining
        foreach (var shard in _matchingEngineShards)
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
        foreach (var shard in _matchingEngineShards)
        {
            await shard.Processor.DisposeAsync();
        }
    }
    #endregion
}
