namespace TradingEngine.MatchingEngine.Interfaces;

/// <summary>
/// Supplies deterministic timestamps for the matching engine.
/// Keep the implementation free of DateTime inside the engine.
/// </summary>
public interface IEngineTimeProvider
{
    long GetTimestamp();
}
