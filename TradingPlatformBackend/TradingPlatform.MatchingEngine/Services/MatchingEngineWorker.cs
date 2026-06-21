using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using TradingEngine.MatchingEngine.Interfaces;
using TradingEngine.MatchingEngine.Commands;
using TradingEngine.MatchingEngine.Models;

namespace TradingEngine.MatchingEngine.Services;

internal sealed class MatchingEngineWorker
{
    private readonly MatchingEngineProcessor _engine;
    private readonly IExecutionResultDispatcher _dispatcher;
    private readonly IExecutionResultStore _resultStore;
    private readonly IEngineTimeProvider _timeProvider;
    private readonly ChannelReader<MatchingEngineQueueItem> _commandReader;
    private readonly ILogger<MatchingEngineWorker> _logger;

    public MatchingEngineWorker(
        MatchingEngineProcessor engine,
        IExecutionResultDispatcher dispatcher,
        IExecutionResultStore resultStore,
        IEngineTimeProvider timeProvider,
        ChannelReader<MatchingEngineQueueItem> commandReader,
        ILogger<MatchingEngineWorker> logger)
    {
        _engine = engine;
        _dispatcher = dispatcher;
        _resultStore = resultStore;
        _timeProvider = timeProvider;
        _commandReader = commandReader;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        await foreach (var item in _commandReader.ReadAllAsync(ct))
        {
            await ProcessCommandAsync(item, ct);
        }
    }

    private async Task ProcessCommandAsync(MatchingEngineQueueItem item, CancellationToken ct)
    {
        var command = item.Command;

        try
        {
            switch (command)
            {
                case SnapshotOrderBookCommand snapshot:
                    var view = _engine.GetSnapshot(snapshot.Symbol);
                    snapshot.Completion.TrySetResult(view);
                    break;

                default:
                    var engineTimestamp = _timeProvider.GetTimestamp();
                    var result = await _engine.ProcessAsync(command, engineTimestamp);

                    if (command.IsDurable)
                    {
                        await WriteDurableResultAsync(command, result, ct);
                    }
                    else
                    {
                        await _dispatcher.DispatchAsync(result, ct);
                    }

                    item.Completion?.TrySetResult(result);
                    break;
            }
        }
        catch (Exception ex)
        {
            item.Completion?.TrySetException(ex);

            if (command is SnapshotOrderBookCommand snapshot)
                snapshot.Completion.TrySetException(ex);

            _logger.LogError(ex, "Failed to process {CommandType} for {Symbol}",
                command.GetType().Name, command.Symbol.Value);
        }
    }

    private async Task WriteDurableResultAsync(
        MatchingEngineCommand command,
        ExecutionResult result,
        CancellationToken cancellationToken)
    {
        var attempt = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _resultStore.WriteAsync(
                    command.CommandOutboxId,
                    result,
                    cancellationToken);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                attempt++;
                var delay = TimeSpan.FromMilliseconds(
                    Math.Min(1000, 25 * attempt));

                _logger.LogError(
                    ex,
                    "Failed to durably store result for command {CommandId}, sequence {SequenceId}. Retrying",
                    command.CommandOutboxId,
                    command.SequenceId);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}
