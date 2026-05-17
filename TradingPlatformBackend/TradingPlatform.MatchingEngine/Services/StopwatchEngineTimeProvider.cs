using System.Diagnostics;
using TradingEngine.MatchingEngine.Interfaces;

namespace TradingEngine.MatchingEngine.Services;

/// <summary>
/// Uses Stopwatch ticks to provide a monotonic timestamp suitable for sequencing.
/// </summary>
public sealed class StopwatchEngineTimeProvider : IEngineTimeProvider
{
    public long GetTimestamp() => Stopwatch.GetTimestamp();
}
