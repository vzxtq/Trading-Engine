using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TradingEngine.Application.Features.Accounts.Commands;
using TradingEngine.Application.Features.Accounts.Dtos;
using TradingEngine.Application.Features.Orders.Commands;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.Enums;
using TradingEngine.Domain.ValueObjects;
using TradingEngine.Infrastructure.Persistence;
using TradingEngineApi.Common;
using Xunit;

namespace TradingPlatform.IntegrationTests;

public class TradeFlowTests : IClassFixture<TradingPlatformFactory>
{
    private readonly TradingPlatformFactory _factory;
    private readonly HttpClient _client;

    public TradeFlowTests(TradingPlatformFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CompleteTradeFlow_ShouldSucceed()
    {
        await _factory.InitializeDatabaseAsync();
        var symbol = IntegrationTestSupport.CreateUniqueSymbol();
        await IntegrationTestSupport.AddSymbolAsync(_factory, symbol);
        var buyerEmail = $"buyer_{Guid.NewGuid()}@test.com";
        var sellerEmail = $"seller_{Guid.NewGuid()}@test.com";
        var testEmails = new[] { buyerEmail, sellerEmail };

        // 1. Register two users (Buyer and Seller)
        var buyerToken = await RegisterAndLoginAsync(buyerEmail, "Buyer123!", "Buyer", "Test");
        var sellerToken = await RegisterAndLoginAsync(sellerEmail, "Seller123!", "Seller", "Test");

        // 1.1 Seed Seller position so they can sell
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
            var seller = await context.UserAccounts.FirstAsync(a => a.Email == sellerEmail);
            var position = PositionDomain.Create(seller.Id, new Symbol(symbol), new Quantity(10.75m), 40000.25m);
            context.Positions.Add(position!);
            await context.SaveChangesAsync();
        }

        // 2. Buyer places a BUY order
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", buyerToken);
        var buyCommand = new PlaceOrderCommand
        {
            Symbol = symbol,
            Price = 50000.25m,
            Quantity = 1.2345m,
            Side = OrderSide.Buy,
            Type = OrderType.Limit
        };
        var buyResponse = await _client.PostAsJsonAsync("/api/orders", buyCommand);
        if (!buyResponse.IsSuccessStatusCode)
        {
            var error = await buyResponse.Content.ReadAsStringAsync();
            throw new Exception($"Buy Order failed: {buyResponse.StatusCode} - {error}");
        }

        // 3. Seller places a SELL order at the same price
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
        var sellCommand = new PlaceOrderCommand
        {
            Symbol = symbol,
            Price = 50000.25m,
            Quantity = 1.2345m,
            Side = OrderSide.Sell,
            Type = OrderType.Limit
        };
        var sellResponse = await _client.PostAsJsonAsync("/api/orders", sellCommand);
        if (!sellResponse.IsSuccessStatusCode)
        {
            var error = await sellResponse.Content.ReadAsStringAsync();
            throw new Exception($"Sell Order failed: {sellResponse.StatusCode} - {error}");
        }

        // 4. Wait for the durable pipeline to settle the trade.
        await IntegrationTestSupport.WaitUntilAsync(async () =>
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
            return await db.Trades
                .Include(x => x.Symbol)
                .AnyAsync(x => x.Symbol.Name == symbol);
        }, failureMessage: $"Trade for {symbol} was not settled.");

        // 5. Verify results in DB
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<TradingDbContext>();

        var testUsers = await verifyContext.UserAccounts
            .Where(a => testEmails.Contains(a.Email))
            .Select(a => a.Id)
            .ToListAsync();

        var orders = await verifyContext.Orders
            .Where(o => testUsers.Contains(o.UserId))
            .ToListAsync();
        orders.Should().HaveCount(2);
        orders.All(o => o.Status == OrderStatus.Filled).Should().BeTrue();

        var trades = await verifyContext.Trades
            .Include(t => t.Symbol)
            .Where(t => testUsers.Contains(t.BuyerId) || testUsers.Contains(t.SellerId))
            .ToListAsync();
        trades.Should().HaveCount(1);
        trades[0].Price.Value.Should().Be(50000.25m);
        trades[0].Quantity.Value.Should().Be(1.2345m);
        trades[0].Symbol.Name.Should().Be(symbol);

        // Verify Position updates
        var sellerId = trades[0].SellerId;
        var buyerId = trades[0].BuyerId;
        var tradedSymbol = new Symbol(symbol);

        var sellerPos = await verifyContext.Positions.FirstOrDefaultAsync(p => p.UserId == sellerId && p.SymbolValue == tradedSymbol);
        sellerPos.Should().NotBeNull();
        sellerPos!.Quantity.Value.Should().Be(9.5155m); // 10.75 seeded - 1.2345 sold
        sellerPos.AverageCost.Should().Be(40000.25m); // Should not change on sell

        var buyerPos = await verifyContext.Positions.FirstOrDefaultAsync(p => p.UserId == buyerId && p.SymbolValue == tradedSymbol);
        buyerPos.Should().NotBeNull();
        buyerPos!.Quantity.Value.Should().Be(1.2345m);
        buyerPos.AverageCost.Should().Be(50000.25m); // 1st purchase at 50000.25

        var buyerAccount = await verifyContext.UserAccounts.FirstAsync(a => a.Id == buyerId);
        var sellerAccount = await verifyContext.UserAccounts.FirstAsync(a => a.Id == sellerId);
        var tradeNotional = 50000.25m * 1.2345m;

        buyerAccount.Balance.Amount.Should().Be(100000m - tradeNotional);
        buyerAccount.ReservedBalance.Amount.Should().Be(0m);
        sellerAccount.Balance.Amount.Should().Be(100000m + tradeNotional);
        (buyerAccount.Balance.Amount + sellerAccount.Balance.Amount).Should().Be(200000m);
    }

    [Fact]
    public async Task CompleteTradeFlow_SellAll_ShouldKeepPositionWithZeroQuantity()
    {
        await _factory.InitializeDatabaseAsync();
        var symbol = IntegrationTestSupport.CreateUniqueSymbol();
        await IntegrationTestSupport.AddSymbolAsync(_factory, symbol);
        var buyerEmail = $"buyer_all_{Guid.NewGuid()}@test.com";
        var sellerEmail = $"seller_all_{Guid.NewGuid()}@test.com";

        var buyerToken = await RegisterAndLoginAsync(buyerEmail, "Buyer123!", "Buyer", "All");
        var sellerToken = await RegisterAndLoginAsync(sellerEmail, "Seller123!", "Seller", "All");

        // Seed Seller position with exactly what they will sell
        Guid sellerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
            var seller = await context.UserAccounts.FirstAsync(a => a.Email == sellerEmail);
            sellerId = seller.Id;
            var position = PositionDomain.Create(sellerId, new Symbol(symbol), new Quantity(5.125m), 150.25m);
            context.Positions.Add(position!);
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", buyerToken);
        var buyResponse = await _client.PostAsJsonAsync("/api/orders", new PlaceOrderCommand { Symbol = symbol, Price = 160.75m, Quantity = 5.125m, Side = OrderSide.Buy, Type = OrderType.Limit });
        buyResponse.EnsureSuccessStatusCode();


        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
        var sellResponse = await _client.PostAsJsonAsync("/api/orders", new PlaceOrderCommand { Symbol = symbol, Price = 160.75m, Quantity = 5.125m, Side = OrderSide.Sell, Type = OrderType.Limit });
        sellResponse.EnsureSuccessStatusCode();

        await IntegrationTestSupport.WaitUntilAsync(async () =>
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
            var position = await db.Positions
                .FirstOrDefaultAsync(p => p.UserId == sellerId && p.SymbolValue == new Symbol(symbol));
            return position?.Quantity.Value == 0;
        }, failureMessage: $"Seller position for {symbol} was not fully settled.");

        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<TradingDbContext>();
        var sellerPos = await verifyContext.Positions.FirstOrDefaultAsync(p => p.UserId == sellerId && p.SymbolValue == new Symbol(symbol));
        
        sellerPos.Should().NotBeNull();
        sellerPos!.Quantity.Value.Should().Be(0);
    }

    private async Task<string> RegisterAndLoginAsync(string email, string password, string first, string last)
    {
        // Register
        var registerCommand = new RegisterUserCommand(email, password, first, last);
        
        var regResp = await _client.PostAsJsonAsync("/api/accounts/register", registerCommand);
        regResp.EnsureSuccessStatusCode();

        // Login
        var loginCommand = new LoginCommand(email, password);
        var logResp = await _client.PostAsJsonAsync("/api/accounts/login", loginCommand);
        logResp.EnsureSuccessStatusCode();

        var content = await logResp.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TradingEngine.Infrastructure.Persistence.TradingDbContext>();
            var acc = await db.UserAccounts.FindAsync(content.Data!.UserId);
            acc!.Deposit(new TradingEngine.Domain.ValueObjects.Money(100000m, Currency.USD));
            await db.SaveChangesAsync();
        }
        
        return content.Data!.Token;
    }
}
