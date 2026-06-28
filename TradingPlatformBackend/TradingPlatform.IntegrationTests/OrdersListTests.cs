using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TradingEngine.Application.Common.Models;
using TradingEngine.Application.Features.Accounts.Commands;
using TradingEngine.Application.Features.Accounts.Dtos;
using TradingEngine.Application.Features.Orders.Commands;
using TradingEngine.Application.Features.Orders.Dtos;
using TradingEngine.Domain.Enums;
using TradingEngineApi.Common;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using TradingEngine.Infrastructure.Persistence;

namespace TradingPlatform.IntegrationTests;

public class OrdersListTests : IClassFixture<TradingPlatformFactory>
{
    private readonly TradingPlatformFactory _factory;
    private readonly HttpClient _client;

    public OrdersListTests(TradingPlatformFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUserOrders_ShouldReturnOrdersAndSummary()
    {
        // 0. Setup
        await _factory.InitializeDatabaseAsync();
        var email = $"orders_{Guid.NewGuid()}@test.com";
        var token = await RegisterAndLoginAsync(email, "Password123!");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Place some orders (all BUY to avoid position validation for now)
        await PlaceOrderAsync("AAPL", 150.25m, 10.125m, OrderSide.Buy);
        await PlaceOrderAsync("MSFT", 300.50m, 5.25m, OrderSide.Buy);
        await PlaceOrderAsync("AAPL", 160.75m, 2.5m, OrderSide.Buy); 

        // 2. Get orders list
        var response = await _client.GetAsync("/api/orders/user-orders?Page=1&PageSize=10");
        response.EnsureSuccessStatusCode();

        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<OrderListResponseDto>>(options);

        // 3. Verify
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().NotBeNull();
        
        // Orders list
        content.Data!.Orders.Items.Should().HaveCount(3);
        content.Data.Orders.TotalCount.Should().Be(3);

        // Summary
        content.Data.Summary.TotalOrders.Should().Be(3);
        content.Data.Summary.OpenOrders.Should().Be(3);
        content.Data.Summary.FilledOrders.Should().Be(0);
        content.Data.Summary.TotalVolume.Should().Be(0); // Nothing filled yet
        content.Data.Summary.FillRate.Should().Be(0);
        content.Data.Summary.CancelledRate.Should().Be(0);
    }

    [Fact]
    public async Task GetUserOrders_WithSorting_ShouldReturnSortedOrders()
    {
        await _factory.InitializeDatabaseAsync();
        var email = $"sort_{Guid.NewGuid()}@test.com";
        var token = await RegisterAndLoginAsync(email, "Password123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await PlaceOrderAsync("AAPL", 100.25m, 10.125m, OrderSide.Buy);
        await PlaceOrderAsync("BTCUSD", 50000.25m, 1.2345m, OrderSide.Buy);
        await PlaceOrderAsync("MSFT", 300.50m, 5.25m, OrderSide.Buy);

        // Sort by Symbol Ascending
        var response = await _client.GetAsync("/api/orders/user-orders?SortingColumn=Symbol&SortingDirection=Ascending");
        response.EnsureSuccessStatusCode();

        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<OrderListResponseDto>>(options);

        content!.Data!.Orders.Items[0].SymbolName.Should().Be("AAPL");
        content.Data.Orders.Items[1].SymbolName.Should().Be("BTCUSD");
        content.Data.Orders.Items[2].SymbolName.Should().Be("MSFT");
        content.Data.Sorting.Should().NotBeNull();
        content.Data.Sorting!.Column.Should().Be("Symbol");
        content.Data.Sorting.Direction.Should().Be(SortingDirection.Ascending);
    }

    private async Task<string> RegisterAndLoginAsync(string email, string password)
    {
        var registerCommand = new RegisterUserCommand(email, password, "Test", "User");
        await _client.PostAsJsonAsync("/api/accounts/register", registerCommand);

        var loginCommand = new LoginCommand(email, password);
        var logResp = await _client.PostAsJsonAsync("/api/accounts/login", loginCommand);
        var content = await logResp.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
            var acc = await db.UserAccounts.FindAsync(content!.Data!.UserId);
            acc!.Deposit(new TradingEngine.Domain.ValueObjects.Money(100000m, Currency.USD));
            await db.SaveChangesAsync();
        }

        return content!.Data!.Token;
    }

    private async Task PlaceOrderAsync(string symbol, decimal price, decimal quantity, OrderSide side)
    {
        var command = new PlaceOrderCommand
        {
            Symbol = symbol,
            Price = price,
            Quantity = quantity,
            Side = side,
            Type = OrderType.Limit
        };
        await _client.PostAsJsonAsync("/api/orders", command);
    }
}
