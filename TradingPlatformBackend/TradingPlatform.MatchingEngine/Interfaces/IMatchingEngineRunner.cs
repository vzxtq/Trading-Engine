using TradingEngine.MatchingEngine.Commands;

namespace TradingEngine.MatchingEngine.Interfaces
{
    public interface IMatchingEngineRunner
    {
        Task RecoverAsync(
            IReadOnlyList<MatchingEngineCommand> commands,
            CancellationToken ct);

        Task RunAsync(CancellationToken ct);
    }
}
