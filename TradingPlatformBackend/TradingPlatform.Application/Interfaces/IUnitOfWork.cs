namespace TradingEngine.Application.Interfaces;

public interface IUnitOfWork
{
    /// <summary>
    /// Executes <paramref name="operation"/> inside a database transaction.
    /// Commits on success, rolls back automatically on failure.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken);
}
