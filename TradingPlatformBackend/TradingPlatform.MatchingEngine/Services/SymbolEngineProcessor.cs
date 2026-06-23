using System.Threading.Channels;
using TradingEngine.MatchingEngine.Commands;
using TradingEngine.MatchingEngine.Models;

namespace TradingEngine.MatchingEngine.Services;

public sealed class SymbolEngineProcessor : IAsyncDisposable
{
    private readonly SymbolEngine _engine;
    private readonly Channel<WorkItem> _mailbox;
    private readonly Task _processTask;
    private readonly CancellationTokenSource _cts = new();
    private long _fallbackSequenceId;

    public SymbolEngineProcessor(string symbol)
    {
        _engine = new SymbolEngine(symbol);
        _mailbox = Channel.CreateUnbounded<WorkItem>(new UnboundedChannelOptions
        {
            SingleReader = true,
            AllowSynchronousContinuations = false
        });

        _processTask = Task.Run(ProcessLoopAsync);
    }

    public async ValueTask<ExecutionResult> EnqueueAsync(MatchingEngineCommand command)
    {
        var sequenceId = command.SequenceId > 0
            ? command.SequenceId
            : Interlocked.Increment(ref _fallbackSequenceId);

        var tcs = new TaskCompletionSource<ExecutionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var workItem = new WorkItem(command, sequenceId, tcs);

        await _mailbox.Writer.WriteAsync(workItem, _cts.Token);

        return await tcs.Task;
    }

    public OrderBookSnapshot Snapshot()
    {
        return _engine.Snapshot();
    }

    private async Task ProcessLoopAsync()
    {
        try
        {
            await foreach (var item in _mailbox.Reader.ReadAllAsync(_cts.Token))
            {
                try
                {
                    var result = _engine.Process(item.Command, item.SequenceId);
                    item.Tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    item.Tcs.SetException(ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _mailbox.Writer.TryComplete();

        try
        {
            await _processTask;
        }
        catch (OperationCanceledException) { }

        _cts.Dispose();
    }

    private record WorkItem(
        MatchingEngineCommand Command,
        long SequenceId,
        TaskCompletionSource<ExecutionResult> Tcs);
}