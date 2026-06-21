using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingEngine.Application.Interfaces;
using TradingEngine.Infrastructure.Persistence;
using TradingEngine.Infrastructure.Persistence.Outbox;
using TradingEngine.MatchingEngine.Interfaces;

namespace TradingEngine.Infrastructure.Services.Outbox;

public sealed class OrderCommandOutboxProcessor : BackgroundService
{
    private static readonly TimeSpan EmptyQueueDelay = TimeSpan.FromMilliseconds(100);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMatchingEngineQueue _matchingEngineQueue;
    private readonly IMatchingEngineReadiness _readiness;
    private readonly ILogger<OrderCommandOutboxProcessor> _logger;

    public OrderCommandOutboxProcessor(
        IServiceScopeFactory scopeFactory,
        IMatchingEngineQueue matchingEngineQueue,
        IMatchingEngineReadiness readiness,
        ILogger<OrderCommandOutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _matchingEngineQueue = matchingEngineQueue;
        _readiness = readiness;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _readiness.WaitUntilReadyAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = await TryDispatchNextAsync(stoppingToken);

            if (!processed)
                await Task.Delay(EmptyQueueDelay, stoppingToken);
        }
    }

    private async Task<bool> TryDispatchNextAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var entry = await dbContext.OrderCommandOutbox
            .Where(x => x.Status == OrderCommandStatus.Pending)
            .OrderBy(x => x.EnqueueId)
            .FirstOrDefaultAsync(cancellationToken);

        if (entry is null)
            return false;

        if (entry.SequenceId is null)
        {
            var sequence = await dbContext.SymbolCommandSequences
                .FirstOrDefaultAsync(
                    x => x.SymbolId == entry.SymbolId,
                    cancellationToken);

            if (sequence is null)
            {
                sequence = SymbolCommandSequence.Create(entry.SymbolId);
                await dbContext.SymbolCommandSequences.AddAsync(sequence, cancellationToken);
            }

            entry.AssignSequence(sequence.AllocateNext());
        }

        entry.MarkDispatched();

        try
        {
            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return true;
        }
        catch (DbUpdateException)
        {
            return true;
        }

        try
        {
            var command = OutboxSerializer.DeserializeCommand(entry);
            await _matchingEngineQueue.EnqueueAsync(command, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            entry.MarkPending(ex.Message);
            await unitOfWork.CommitAsync(cancellationToken);

            _logger.LogError(
                ex,
                "Failed to dispatch order command {CommandId} with sequence {SequenceId}",
                entry.Id,
                entry.SequenceId);

            return false;
        }
    }
}
