namespace TradingEngine.MatchingEngine.Interfaces;

public interface IMatchingEngineReadiness
{
    Task WaitUntilReadyAsync(CancellationToken cancellationToken);
}
