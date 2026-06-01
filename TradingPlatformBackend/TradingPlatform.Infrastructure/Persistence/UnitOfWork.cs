using Microsoft.EntityFrameworkCore.Storage;
using TradingEngine.Application.Interfaces;

namespace TradingEngine.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly TradingDbContext _dbContext;

    public UnitOfWork(TradingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    //TODO not sure about this auto rollback
    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            T result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
