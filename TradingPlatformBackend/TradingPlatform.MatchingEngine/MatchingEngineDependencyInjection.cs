using Microsoft.Extensions.DependencyInjection;
using TradingEngine.MatchingEngine.Interfaces;
using TradingEngine.MatchingEngine.Services;
using TradingEngine.MatchingEngine.Services.Background;

namespace TradingEngine.MatchingEngine;

public static class MatchingEngineDependencyInjection
{
    public static MatchingEngineBuilder AddMatchingEngine(
        this IServiceCollection services,
        Action<MatchingEngineOptions>? configure = null)
    {
        var defaults = new MatchingEngineOptions();

        services.AddOptions<MatchingEngineOptions>()
            .Configure(options =>
            {
                options.ChannelCapacity = defaults.ChannelCapacity;
                options.ShardCount = defaults.ShardCount;
                options.FullMode = defaults.FullMode;
            });

        if (configure is not null)
            services.Configure(configure);

        services.AddSingleton<IExecutionResultDispatcher, ExecutionResultDispatcher>();
        services.AddSingleton<MatchingEngineHost>();
        services.AddSingleton<IMatchingEngineQueue>(sp => sp.GetRequiredService<MatchingEngineHost>());
        services.AddSingleton<IMatchingEngineCommandExecutor>(sp => sp.GetRequiredService<MatchingEngineHost>());
        services.AddSingleton<IOrderBookSnapshotProvider>(sp => sp.GetRequiredService<MatchingEngineHost>());
        services.AddSingleton<IMatchingEngineRunner>(sp => sp.GetRequiredService<MatchingEngineHost>());
        services.AddSingleton<IMatchingEngineReadiness>(sp => sp.GetRequiredService<MatchingEngineHost>());
        services.AddHostedService<MatchingEngineBackgroundService>();

        return new MatchingEngineBuilder(services);
    }

    public static MatchingEngineBuilder AddHandler<THandler>(this MatchingEngineBuilder builder)
        where THandler : class, IExecutionResultHandler
    {
        builder.Services.AddScoped<IExecutionResultHandler, THandler>();
        return builder;
    }
}
