using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using TradingEngine.MatchingEngine.Interfaces;
using TradingEngine.MatchingEngine.Commands;

namespace TradingEngine.MatchingEngine.Services;

public sealed class MatchingEngineWorker
{
    private readonly MatchingEngineProcessor _engine;
    private readonly IExecutionResultDispatcher _dispatcher;
    private readonly IEngineTimeProvider _timeProvider;
    private readonly ChannelReader<MatchingEngineCommand> _commandReader;
    private readonly ILogger<MatchingEngineWorker> _logger;

    public MatchingEngineWorker(
        MatchingEngineProcessor engine,
        IExecutionResultDispatcher dispatcher,
        IEngineTimeProvider timeProvider,
        ChannelReader<MatchingEngineCommand> commandReader,
        ILogger<MatchingEngineWorker> logger)
    {
        _engine = engine;
        _dispatcher = dispatcher;
        _timeProvider = timeProvider;
        _commandReader = commandReader;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        await foreach (var command in _commandReader.ReadAllAsync(ct))
        {
            await ProcessCommandAsync(command, ct);
        }
    }

    private async Task ProcessCommandAsync(MatchingEngineCommand command, CancellationToken ct)
    {
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
                    await _dispatcher.DispatchAsync(result, ct);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {CommandType} for {Symbol}",
                command.GetType().Name, command.Symbol.Value);
        }
    }
}
