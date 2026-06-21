using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingEngine.MatchingEngine.Interfaces;

namespace TradingEngine.MatchingEngine.Services.Background;

public sealed class MatchingEngineBackgroundService : BackgroundService
{
    private readonly IMatchingEngineRunner _matchingEngineRunner;
    private readonly IMatchingEngineRecoverySource _recoverySource;
    private readonly ILogger<MatchingEngineBackgroundService> _logger;

    public MatchingEngineBackgroundService(
        IMatchingEngineRunner runner,
        IMatchingEngineRecoverySource recoverySource,
        ILogger<MatchingEngineBackgroundService> logger)
    {
        _matchingEngineRunner = runner;
        _recoverySource = recoverySource;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Matching Engine starting");
        try
        {
            var commands = await _recoverySource.LoadCompletedCommandsAsync(stoppingToken);
            await _matchingEngineRunner.RecoverAsync(commands, stoppingToken);

            _logger.LogInformation("Matching Engine recovered {CommandCount} durable commands", commands.Count);

            await _matchingEngineRunner.RunAsync(stoppingToken);
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
