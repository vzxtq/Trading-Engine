using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TradingEngine.Application.Interfaces;
using TradingEngine.Infrastructure.Persistence;
using TradingEngine.Infrastructure.Persistence.Outbox;
using TradingEngine.MatchingEngine.Interfaces;
using TradingEngine.MatchingEngine.Models;

namespace TradingEngine.Infrastructure.Services.Outbox;

public sealed class ExecutionResultStore : IExecutionResultStore
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ExecutionResultStore(IServiceScopeFactory scopeFactory)
    {
        _serviceScopeFactory = scopeFactory;
    }

    public async Task WriteAsync(
        Guid commandOutboxId,
        ExecutionResult result,
        CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var existingResult = await dbContext.ExecutionResultOutbox
                .FirstOrDefaultAsync(x => x.CommandOutboxId == commandOutboxId, ct);

            var command = await dbContext.OrderCommandOutbox
                .FirstOrDefaultAsync(x => x.Id == commandOutboxId, ct)
                ?? throw new InvalidOperationException($"Order command outbox entry {commandOutboxId} was not found.");

            if (existingResult is null)
            {
                var entry = ExecutionResultOutboxEntry.Create(
                    commandOutboxId,
                    result.SymbolId,
                    result.SequenceId,
                    OutboxSerializer.GetExecutionResultType(result),
                    OutboxSerializer.SerializeExecutionResult(result));

                await dbContext.ExecutionResultOutbox.AddAsync(entry, ct);
            }

            command.MarkCompleted();
            await unitOfWork.CommitAsync(ct);
            return true;
        }, cancellationToken);
    }
}
