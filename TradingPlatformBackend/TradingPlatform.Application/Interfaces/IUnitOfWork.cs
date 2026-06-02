namespace TradingEngine.Application.Interfaces;

public interface IUnitOfWork
{
    /// <summary>
    /// Persists all staged changes to the database in a single round-trip.
    /// Call this once at the end of a use-case after all repository staging calls.
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes <paramref name="operation"/> inside a database transaction.
    /// Commits on success, rolls back automatically on failure.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken);
}
