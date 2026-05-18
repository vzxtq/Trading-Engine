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
        // 0. Setup database and toggle SQLite vs Real
        // _factory.UseSqlite = true; // Set to true if you want to go back to SQLite
        await _factory.InitializeDatabaseAsync();
        var testEmails = new[] { "buyer@test.com", "seller@test.com" };

        // 0.1 Clean up existing test data to ensure repeatability
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
            var existingAccounts = await context.UserAccounts
                .Where(a => testEmails.Contains(a.Email))
                .ToListAsync();

            if (existingAccounts.Any())
            {
                var accountIds = existingAccounts.Select(a => a.Id).ToList();
                
                // Remove related data (Cascading delete or manual)
                var existingOrders = await context.Orders.Where(o => accountIds.Contains(o.UserId)).ToListAsync();
                context.Orders.RemoveRange(existingOrders);

                var existingTrades = await context.Trades.Where(t => accountIds.Contains(t.BuyerId) || accountIds.Contains(t.SellerId)).ToListAsync();
                context.Trades.RemoveRange(existingTrades);
                
                var existingIdentities = await context.UserIdentities.Where(i => accountIds.Contains(i.UserId)).ToListAsync();
                context.UserIdentities.RemoveRange(existingIdentities);

                var existingPositions = await context.Positions.Where(p => accountIds.Contains(p.UserId)).ToListAsync();
                context.Positions.RemoveRange(existingPositions);

                context.UserAccounts.RemoveRange(existingAccounts);
                await context.SaveChangesAsync();
            }
        }

        // 1. Register two users (Buyer and Seller)
        var buyerToken = await RegisterAndLoginAsync("buyer@test.com", "Buyer123!", "Buyer", "Test");
        var sellerToken = await RegisterAndLoginAsync("seller@test.com", "Seller123!", "Seller", "Test");

        // 1.1 Seed Seller position so they can sell
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
            var seller = await context.UserAccounts.FirstAsync(a => a.Email == "seller@test.com");
            var position = PositionDomain.Create(seller.Id, new Symbol("BTCUSD"), new Quantity(10), 40000);
            context.Positions.Add(position!);
            await context.SaveChangesAsync();
        }

        // 2. Buyer places a BUY order
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", buyerToken);
        var buyCommand = new PlaceOrderCommand
        {
            Symbol = "BTCUSD",
            Price = 50000,
            Quantity = 1,
            Side = OrderSide.Buy
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
            Symbol = "BTCUSD",
            Price = 50000,
            Quantity = 1,
            Side = OrderSide.Sell
        };
        var sellResponse = await _client.PostAsJsonAsync("/api/orders", sellCommand);
        if (!sellResponse.IsSuccessStatusCode)
        {
            var error = await sellResponse.Content.ReadAsStringAsync();
            throw new Exception($"Sell Order failed: {sellResponse.StatusCode} - {error}");
        }

        // 4. Wait for matching engine to process (background task)
        await Task.Delay(2000);

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
        trades[0].Price.Value.Should().Be(50000);
        trades[0].Quantity.Value.Should().Be(1);
        trades[0].Symbol.Name.Should().Be("BTCUSD");

        // Verify Position updates
        var sellerId = trades[0].SellerId;
        var buyerId = trades[0].BuyerId;
        var btcSymbol = new Symbol("BTCUSD");

        var sellerPos = await verifyContext.Positions.FirstOrDefaultAsync(p => p.UserId == sellerId && p.SymbolValue == btcSymbol);
        sellerPos.Should().NotBeNull();
        sellerPos!.Quantity.Value.Should().Be(9); // 10 seeded - 1 sold
        sellerPos.AverageCost.Should().Be(40000); // Should not change on sell

        var buyerPos = await verifyContext.Positions.FirstOrDefaultAsync(p => p.UserId == buyerId && p.SymbolValue == btcSymbol);
        buyerPos.Should().NotBeNull();
        buyerPos!.Quantity.Value.Should().Be(1);
        buyerPos.AverageCost.Should().Be(50000); // 1st purchase at 50000
    }

    [Fact]
    public async Task CompleteTradeFlow_SellAll_ShouldKeepPositionWithZeroQuantity()
    {
        await _factory.InitializeDatabaseAsync();
        var testEmails = new[] { "buyer_all@test.com", "seller_all@test.com" };

        // Clean up
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
            var accounts = await context.UserAccounts.Where(a => testEmails.Contains(a.Email)).ToListAsync();
            foreach (var acc in accounts)
            {
                var accountIds = new[] { acc.Id };
                context.Orders.RemoveRange(await context.Orders.Where(o => accountIds.Contains(o.UserId)).ToListAsync());
                context.Trades.RemoveRange(await context.Trades.Where(t => accountIds.Contains(t.BuyerId) || accountIds.Contains(t.SellerId)).ToListAsync());
                context.UserIdentities.RemoveRange(await context.UserIdentities.Where(i => accountIds.Contains(i.UserId)).ToListAsync());
                context.Positions.RemoveRange(await context.Positions.Where(p => accountIds.Contains(p.UserId)).ToListAsync());
            }
            context.UserAccounts.RemoveRange(accounts);
            await context.SaveChangesAsync();
        }

        var buyerToken = await RegisterAndLoginAsync("buyer_all@test.com", "Buyer123!", "Buyer", "All");
        var sellerToken = await RegisterAndLoginAsync("seller_all@test.com", "Seller123!", "Seller", "All");

        // Seed Seller position with exactly what they will sell
        Guid sellerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
            var seller = await context.UserAccounts.FirstAsync(a => a.Email == "seller_all@test.com");
            sellerId = seller.Id;
            var position = PositionDomain.Create(sellerId, new Symbol("AAPL"), new Quantity(5), 150);
            context.Positions.Add(position!);
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", buyerToken);
        await _client.PostAsJsonAsync("/api/orders", new PlaceOrderCommand { Symbol = "AAPL", Price = 160, Quantity = 5, Side = OrderSide.Buy });

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
        await _client.PostAsJsonAsync("/api/orders", new PlaceOrderCommand { Symbol = "AAPL", Price = 160, Quantity = 5, Side = OrderSide.Sell });

        await Task.Delay(2000);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<TradingDbContext>();
        var sellerPos = await verifyContext.Positions.FirstOrDefaultAsync(p => p.UserId == sellerId && p.SymbolValue == new Symbol("AAPL"));
        
        sellerPos.Should().NotBeNull();
        sellerPos!.Quantity.Value.Should().Be(0);
    }

    private async Task<string> RegisterAndLoginAsync(string email, string password, string first, string last)
    {
        // Register
        var registerCommand = new RegisterUserCommand(
            email, password, first, last, 100000, Currency.USD);
        
        var regResp = await _client.PostAsJsonAsync("/api/accounts/register", registerCommand);
        regResp.EnsureSuccessStatusCode();

        // Login
        var loginCommand = new LoginCommand(email, password);
        var logResp = await _client.PostAsJsonAsync("/api/accounts/login", loginCommand);
        logResp.EnsureSuccessStatusCode();

        var content = await logResp.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        
        return content.Data!.Token;
    }
}
