using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingEngine.MatchingEngine.Interfaces;

namespace TradingEngine.MatchingEngine.Services.Background;

public sealed class MatchingEngineBackgroundService : BackgroundService
{
    private readonly IMatchingEngineRunner _runner;
    private readonly ILogger<MatchingEngineBackgroundService> _logger;

    public MatchingEngineBackgroundService(
        IMatchingEngineRunner runner,
        ILogger<MatchingEngineBackgroundService> logger)
    {
        _runner = runner;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Matching Engine starting");
        try
        {
            await _runner.RunAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Matching Engine fatal error");
            throw;
        }
        finally
        {
            _logger.LogInformation("Matching Engine stopped");
        }
    }
}
