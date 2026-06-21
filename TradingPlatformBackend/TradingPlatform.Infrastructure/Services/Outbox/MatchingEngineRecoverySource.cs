using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TradingEngine.Application.Interfaces;
using TradingEngine.Infrastructure.Persistence;
using TradingEngine.Infrastructure.Persistence.Outbox;
using TradingEngine.MatchingEngine.Commands;
using TradingEngine.MatchingEngine.Interfaces;

namespace TradingEngine.Infrastructure.Services.Outbox;

public sealed class MatchingEngineRecoverySource : IMatchingEngineRecoverySource
{
    private readonly IServiceScopeFactory _scopeFactory;

    public MatchingEngineRecoverySource(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<IReadOnlyList<MatchingEngineCommand>> LoadCompletedCommandsAsync(
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var interruptedCommands = await dbContext.OrderCommandOutbox
            .Where(x => x.Status == OrderCommandStatus.Dispatched)
            .Where(x => !dbContext.ExecutionResultOutbox
                .Any(r => r.CommandOutboxId == x.Id))
           .ToListAsync(cancellationToken);

        if (interruptedCommands.Count > 0)
        {
            foreach (var command in interruptedCommands)
                command.MarkPending("Recovered after an interrupted dispatch.");

            await unitOfWork.CommitAsync(cancellationToken);
        }

        var completedCommands = await dbContext.OrderCommandOutbox
            .AsNoTracking()
            .Where(x => x.Status == OrderCommandStatus.Completed)
            .OrderBy(x => x.SymbolId)
            .ThenBy(x => x.SequenceId)
            .ToListAsync(cancellationToken);

        return completedCommands
            .Select(OutboxSerializer.DeserializeCommand)
            .ToList();
    }
}
