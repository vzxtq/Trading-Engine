using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.Enums;
using TradingEngine.Infrastructure.Persistence;

namespace TradingPlatform.IntegrationTests;

internal static class IntegrationTestSupport
{
    public static string CreateUniqueSymbol()
    {
        var bytes = Guid.NewGuid().ToByteArray();
        return string.Concat(bytes
            .Take(9)
            .Select(value => (char)('A' + (value % 26))));
    }

    public static async Task AddSymbolAsync(
        TradingPlatformFactory factory,
        string symbol)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingDbContext>();

        if (!await dbContext.Symbols.AnyAsync(x => x.Name == symbol))
        {
            dbContext.Symbols.Add(SymbolDomain.Create(symbol, Currency.USD));
            await dbContext.SaveChangesAsync();
        }
    }

    public static async Task WaitUntilAsync(
        Func<Task<bool>> condition,
        TimeSpan? timeout = null,
        string? failureMessage = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));

        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
                return;

            await Task.Delay(100);
        }

        throw new TimeoutException(
            failureMessage ?? "Timed out waiting for the expected integration-test state.");
    }
}
