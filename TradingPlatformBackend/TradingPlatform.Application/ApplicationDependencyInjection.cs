using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TradingEngine.Application.Features.Orders.Placement;
using TradingEngine.Application.Features.Orders.Placement.Strategies;
using TradingEngine.Application.Interfaces.OrderPlacement;
using TradingEngine.MatchingEngine;
using TradingEngine.MatchingEngine.Handlers;

namespace TradingEngine.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationDependencyInjection).Assembly));

        services.AddScoped<IOrderPlacementStrategy, LimitBuyOrderPlacementStrategy>();
        services.AddScoped<IOrderPlacementStrategy, MarketBuyOrderPlacementStrategy>();
        services.AddScoped<IOrderPlacementStrategy, LimitSellOrderPlacementStrategy>();
        services.AddScoped<IOrderPlacementStrategy, MarketSellOrderPlacementStrategy>();
        services.AddScoped<
            IOrderPlacementStrategySelector,
            OrderPlacementStrategySelector>();

        services.AddMatchingEngine()
                .AddHandler<MarketDataExecutionResultHandler>();

        return services;
    }
}
