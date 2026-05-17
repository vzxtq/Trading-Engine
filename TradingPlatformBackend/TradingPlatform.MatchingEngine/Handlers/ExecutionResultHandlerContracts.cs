using TradingEngine.MatchingEngine.Interfaces;

namespace TradingEngine.MatchingEngine.Handlers;

/// <summary>
/// Handler that persists execution results to the database.
/// Responsibilities:
/// - Insert executed trades into the trades table
/// - Update order statuses in the orders table
/// - Maintain account balances
/// </summary>
public interface IPersistenceExecutionResultHandler : IExecutionResultHandler
{
}

/// <summary>
/// Handler that publishes market data updates (e.g., via SignalR or pub-sub).
/// Responsibilities:
/// - Broadcast executed trades to connected clients
/// - Update order book snapshots
/// - Publish trade ticker events
/// </summary>
public interface IMarketDataExecutionResultHandler : IExecutionResultHandler
{
}

/// <summary>
/// Handler that updates account state based on execution results.
/// Responsibilities:
/// - Debit buyer's account on successful purchase
/// - Credit seller's account on successful sale
/// - Manage reserved funds for pending orders
/// </summary>
public interface IAccountExecutionResultHandler : IExecutionResultHandler
{
}
