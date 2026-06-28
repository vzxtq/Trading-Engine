using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingEngine.Application.Interfaces;
using TradingEngine.Infrastructure.Handlers;
using TradingEngine.Infrastructure.Persistence;
using TradingEngine.Infrastructure.Persistence.Outbox;
using TradingEngine.MatchingEngine.Interfaces;

namespace TradingEngine.Infrastructure.Services.Outbox;

public sealed class ExecutionResultOutboxProcessor : BackgroundService
{
    private const int MaxProcessingAttempts = 3;
    private static readonly TimeSpan EmptyQueueDelay = TimeSpan.FromSeconds(5);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PersistenceExecutionResultHandler _persistenceHandler;
    private readonly IExecutionResultDispatcher _dispatcher;
    private readonly ILogger<ExecutionResultOutboxProcessor> _logger;

    public ExecutionResultOutboxProcessor(
        IServiceScopeFactory scopeFactory,
        PersistenceExecutionResultHandler persistenceHandler,
        IExecutionResultDispatcher dispatcher,
        ILogger<ExecutionResultOutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _persistenceHandler = persistenceHandler;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = await TryProcessNextAsync(stoppingToken);
            if (!processed)
                await Task.Delay(EmptyQueueDelay, stoppingToken);
        }
    }

    private async Task<bool> TryProcessNextAsync(CancellationToken cancellationToken)
    {
        Guid outboxId;
        string resultType;
        string payload;

        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
            var entry = await dbContext.ExecutionResultOutbox
                .AsNoTracking()
                .Where(x => x.Status == ExecutionResultOutboxStatus.Pending)
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (entry is null)
                return false;

            outboxId = entry.Id;
            resultType = entry.ResultType;
            payload = entry.Payload;
        }

        try
        {
            var result = OutboxSerializer.DeserializeExecutionResult(resultType, payload);
            await _persistenceHandler.ProcessAsync(outboxId, result, cancellationToken);
            await _dispatcher.DispatchAsync(result, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process execution result outbox entry {OutboxId}",
                outboxId);

            await RecordFailureAsync(outboxId, ex.Message, cancellationToken);
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            return false;
        }
    }

    private async Task RecordFailureAsync(
        Guid outboxId,
        string error,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<TradingDbContext>();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var entry = await dbContext.ExecutionResultOutbox
            .FirstOrDefaultAsync(x => x.Id == outboxId, cancellationToken);

        if (entry is null)
            return;

        entry.RecordFailure(error);

        if (entry.AttemptCount >= MaxProcessingAttempts)
            entry.MarkFailed(error);

        await unitOfWork.CommitAsync(cancellationToken);
    }
}