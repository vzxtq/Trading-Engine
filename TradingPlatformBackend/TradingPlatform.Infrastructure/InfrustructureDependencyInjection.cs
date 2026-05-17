using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TradingEngine.Application.Features.Orders.Repositories;
using TradingEngine.Application.Interfaces;
using TradingEngine.Application.Interfaces.Accounts;
using TradingEngine.Application.Interfaces.Orders;
using TradingEngine.Application.Interfaces.Positions;
using TradingEngine.Application.Interfaces.Symbols;
using TradingEngine.Application.Interfaces.Trades;
using TradingEngine.Application.Options;
using TradingEngine.Infrastructure.Handlers;
using TradingEngine.Infrastructure.Persistence;
using TradingEngine.Infrastructure.Repositories;
using TradingEngine.Infrastructure.Repositories.Accounts;
using TradingEngine.Infrastructure.Repositories.Orders;
using TradingEngine.Infrastructure.Repositories.Positions;
using TradingEngine.Infrastructure.Repositories.Trades;
using TradingEngine.Infrastructure.Security;
using TradingEngine.MatchingEngine.Interfaces;

namespace TradingEngine.Infrastructure;

public static class InfrustructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<DatabaseSettings>()
            .Bind(configuration.GetSection(DatabaseSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddDbContext<TradingDbContext>((sp, options) =>
        {
            var dbOptions = sp.GetRequiredService<IOptions<DatabaseSettings>>().Value;
            options.UseSqlServer(dbOptions.DefaultConnection,
                sql => sql.MigrationsAssembly(DatabaseSettings.MigrationsAssembly));
        });

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IUserIdentityRepository, UserIdentityRepository>();
        services.AddScoped<IAccountReadRepository, AccountReadRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderReadRepository, OrderReadRepository>();
        services.AddScoped<IOrderBookReadRepository, OrderBookReadRepository>();
        services.AddScoped<IPositionRepository, PositionRepository>();
        services.AddScoped<IPositionReadRepository, PositionReadRepository>();
        services.AddScoped<ITradeReadRepository, TradeReadRepository>();
        services.AddScoped<ISymbolReadRepository, SymbolReadRepository>();
        services.AddScoped<IExecutionResultHandler, PersistenceExecutionResultHandler>();

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
