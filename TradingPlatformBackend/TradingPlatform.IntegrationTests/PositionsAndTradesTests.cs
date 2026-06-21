using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TradingEngine.Application.Features.Accounts.Commands;
using TradingEngine.Application.Features.Accounts.Dtos;
using TradingEngine.Application.Features.Orders.Commands;
using TradingEngine.Application.Features.Positions.Dtos;
using TradingEngine.Application.Features.Trades.Dtos;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.Enums;
using TradingEngine.Domain.ValueObjects;
using TradingEngine.Infrastructure.Persistence;
using TradingEngineApi.Common;
using Xunit;

namespace TradingPlatform.IntegrationTests;

public class PositionsAndTradesTests : IClassFixture<TradingPlatformFactory>
{
    private readonly TradingPlatformFactory _factory;
    private readonly HttpClient _client;

    public PositionsAndTradesTests(TradingPlatformFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PositionsAndTrades_ShouldBeUpdatedAfterTrade()
    {
        // 0. Setup
        await _factory.InitializeDatabaseAsync();
        var symbol = IntegrationTestSupport.CreateUniqueSymbol();
        await IntegrationTestSupport.AddSymbolAsync(_factory, symbol);
        decimal price = 150.95m;
        decimal quantity = 10.5m;

        // 1. Register two users
        var buyerEmail = $"buyer_{Guid.NewGuid()}@test.com";
        var sellerEmail = $"seller_{Guid.NewGuid()}@test.com";
        
        var buyerToken = await RegisterAndLoginAsync(buyerEmail, "Password123!", "Buyer", "Test");
        var sellerToken = await RegisterAndLoginAsync(sellerEmail, "Password123!", "Seller", "Test");

        // 1.1 Seed Seller position so they can sell
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
            var seller = context.UserAccounts.First(a => a.Email == sellerEmail);
            var position = PositionDomain.Create(seller.Id, new Symbol(symbol), new Quantity(quantity), price);
            context.Positions.Add(position!);
            await context.SaveChangesAsync();
        }

        // 2. Buyer places a BUY order
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", buyerToken);
        var buyCommand = new PlaceOrderCommand
        {
            Symbol = symbol,
            Price = price,
            Quantity = quantity,
            Side = OrderSide.Buy,
            Type = OrderType.Limit
        };
        var buyResponse = await _client.PostAsJsonAsync("/api/orders", buyCommand);
        buyResponse.EnsureSuccessStatusCode();

        // 3. Seller places a SELL order at the same price
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
        var sellCommand = new PlaceOrderCommand
        {
            Symbol = symbol,
            Price = price,
            Quantity = quantity,
            Side = OrderSide.Sell,
            Type = OrderType.Limit
        };
        var sellResponse = await _client.PostAsJsonAsync("/api/orders", sellCommand);
        sellResponse.EnsureSuccessStatusCode();

        // 4. Wait for the durable pipeline to settle the trade.
        await IntegrationTestSupport.WaitUntilAsync(async () =>
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
            return await db.Trades
                .Include(x => x.Symbol)
                .AnyAsync(x => x.Symbol.Name == symbol);
        }, failureMessage: $"Trade for {symbol} was not settled.");

        // 5. Verify Positions for Buyer
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", buyerToken);
        var buyerPositionsResp = await _client.GetAsync("/api/accounts/positions");
        buyerPositionsResp.EnsureSuccessStatusCode();
        var buyerPositions = await buyerPositionsResp.Content.ReadFromJsonAsync<ApiResponse<List<PositionDto>>>();
        
        buyerPositions!.Data.Should().NotBeNull();
        buyerPositions.Data.Should().ContainSingle(p => p.Symbol == symbol);
        buyerPositions.Data!.First(p => p.Symbol == symbol).Quantity.Should().Be(quantity);

        // 6. Verify Trades for Buyer
        var buyerTradesResp = await _client.GetAsync("/api/accounts/trades");
        buyerTradesResp.EnsureSuccessStatusCode();
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
        var buyerTrades = await buyerTradesResp.Content.ReadFromJsonAsync<ApiResponse<TradeHistoryResponseDto>>(options);
        
        buyerTrades!.Data.Should().NotBeNull();
        buyerTrades.Data!.Trades.Items.Should().ContainSingle(t => t.Symbol == symbol);
        buyerTrades.Data!.Trades.Items.First(t => t.Symbol == symbol).Quantity.Should().Be(quantity);
        buyerTrades.Data!.Trades.Items.First(t => t.Symbol == symbol).Price.Should().Be(price);

        // 7. Verify Positions for Seller
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);
        var sellerPositionsResp = await _client.GetAsync("/api/accounts/positions");
        sellerPositionsResp.EnsureSuccessStatusCode();
        var sellerPositions = await sellerPositionsResp.Content.ReadFromJsonAsync<ApiResponse<List<PositionDto>>>(options);
        
        // Seller had 10, sold 10, so position should be 0 or removed depending on implementation
        var sellerSymbolPosition = sellerPositions!.Data?.FirstOrDefault(p => p.Symbol == symbol);
        if (sellerSymbolPosition != null)
        {
            sellerSymbolPosition.Quantity.Should().Be(0);
        }

        // 8. Verify Trades for Seller
        var sellerTradesResp = await _client.GetAsync("/api/accounts/trades");
        sellerTradesResp.EnsureSuccessStatusCode();
        var sellerTrades = await sellerTradesResp.Content.ReadFromJsonAsync<ApiResponse<TradeHistoryResponseDto>>(options);
        
        sellerTrades!.Data.Should().NotBeNull();
        sellerTrades.Data!.Trades.Items.Should().ContainSingle(t => t.Symbol == symbol);
        sellerTrades.Data!.Trades.Items.First(t => t.Symbol == symbol).Quantity.Should().Be(quantity);
        sellerTrades.Data!.Trades.Items.First(t => t.Symbol == symbol).Price.Should().Be(price);
    }

    private async Task<string> RegisterAndLoginAsync(string email, string password, string first, string last)
    {
        var registerCommand = new RegisterUserCommand(email, password, first, last);
        var regResp = await _client.PostAsJsonAsync("/api/accounts/register", registerCommand);
        regResp.EnsureSuccessStatusCode();

        var loginCommand = new LoginCommand(email, password);
        var logResp = await _client.PostAsJsonAsync("/api/accounts/login", loginCommand);
        logResp.EnsureSuccessStatusCode();

        var content = await logResp.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        content.Should().NotBeNull();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TradingEngine.Infrastructure.Persistence.TradingDbContext>();
            var acc = await db.UserAccounts.FindAsync(content!.Data!.UserId);
            acc!.Deposit(new TradingEngine.Domain.ValueObjects.Money(100000m, Currency.USD));
            await db.SaveChangesAsync();
        }

        return content!.Data!.Token;
    }
}
