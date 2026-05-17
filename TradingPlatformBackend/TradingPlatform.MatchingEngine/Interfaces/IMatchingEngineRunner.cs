namespace TradingEngine.MatchingEngine.Interfaces
{
    public interface IMatchingEngineRunner
    {
        Task RunAsync(CancellationToken ct);
    }
}
