using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TradingEngine.Application.Features.Accounts.Commands;
using TradingEngine.Application.Features.Accounts.Dtos;
using TradingEngine.Application.Features.Orders.Commands;
using TradingEngine.Application.Features.Orders.Dtos;
using TradingEngine.Domain.Enums;
using TradingEngine.Domain.ValueObjects;
using TradingEngine.Infrastructure.Persistence;
using TradingEngine.Infrastructure.Persistence.Outbox;
using TradingEngineApi.Common;

namespace TradingPlatform.IntegrationTests;

public sealed class OutboxPipelineTests : IClassFixture<TradingPlatformFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly TradingPlatformFactory _factory;
    private readonly HttpClient _client;

    public OutboxPipelineTests(TradingPlatformFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PlaceOrder_ShouldPersistAndSettleDurableOutboxCommand()
    {
        await _factory.InitializeDatabaseAsync();
        var symbol = IntegrationTestSupport.CreateUniqueSymbol();
        await IntegrationTestSupport.AddSymbolAsync(_factory, symbol);
        var token = await RegisterAndFundAsync($"outbox_{Guid.NewGuid()}@test.com");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/orders", new PlaceOrderCommand
        {
            Symbol = symbol,
            Price = 125.50m,
            Quantity = 2m,
            Side = OrderSide.Buy,
            Type = OrderType.Limit
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PlaceOrderResponseDto>>(JsonOptions);
        var orderId = body!.Data!.OrderId;
        Guid commandId;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
            var command = await db.OrderCommandOutbox
                .SingleAsync(x => x.OrderId == orderId
                                  && x.CommandType == OrderCommandType.AddOrder);

            command.Payload.Should().Contain(orderId.ToString());
            command.EnqueueId.Should().BePositive();
            commandId = command.Id;
        }

        await IntegrationTestSupport.WaitUntilAsync(async () =>
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
            return await db.OrderCommandOutbox
                .Where(x => x.OrderId == orderId
                            && x.CommandType == OrderCommandType.AddOrder)
                .AnyAsync(x => x.Status == OrderCommandStatus.Completed
                               && x.SequenceId != null)
                   && await db.ExecutionResultOutbox
                       .AnyAsync(x => x.CommandOutboxId == commandId
                                      && x.Status == ExecutionResultOutboxStatus.Processed);
        }, failureMessage: "The add-order outbox command was not durably processed.");
    }

    [Fact]
    public async Task CancelOrder_ShouldReturnAcceptedAndReleaseReservedFundsEventually()
    {
        await _factory.InitializeDatabaseAsync();
        var symbol = IntegrationTestSupport.CreateUniqueSymbol();
        await IntegrationTestSupport.AddSymbolAsync(_factory, symbol);
        var email = $"cancel_{Guid.NewGuid()}@test.com";
        var token = await RegisterAndFundAsync(email);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var placeResponse = await _client.PostAsJsonAsync("/api/orders", new PlaceOrderCommand
        {
            Symbol = symbol,
            Price = 100m,
            Quantity = 10m,
            Side = OrderSide.Buy,
            Type = OrderType.Limit
        });
        placeResponse.EnsureSuccessStatusCode();

        var placeBody = await placeResponse.Content
            .ReadFromJsonAsync<ApiResponse<PlaceOrderResponseDto>>(JsonOptions);
        var orderId = placeBody!.Data!.OrderId;

        await IntegrationTestSupport.WaitUntilAsync(async () =>
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
            return await db.OrderCommandOutbox.AnyAsync(
                x => x.OrderId == orderId
                     && x.CommandType == OrderCommandType.AddOrder
                     && x.Status == OrderCommandStatus.Completed);
        });

        var cancelResponse = await _client.PostAsJsonAsync(
            $"/api/orders/{orderId}/cancel",
            new CancelOrderCommand());

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var cancelBody = await cancelResponse.Content
            .ReadFromJsonAsync<ApiResponse<CancelOrderResponseDto>>(JsonOptions);
        cancelBody!.Data!.OrderId.Should().Be(orderId);
        cancelBody.Message.Should().Be("Cancellation queued");

        await IntegrationTestSupport.WaitUntilAsync(async () =>
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
            var order = await db.Orders.SingleAsync(x => x.Id == orderId);
            var account = await db.UserAccounts.SingleAsync(x => x.Email == email);
            var cancelSettled = await db.OrderCommandOutbox.AnyAsync(
                x => x.OrderId == orderId
                     && x.CommandType == OrderCommandType.CancelOrder
                     && x.Status == OrderCommandStatus.Completed
                     && x.ActiveCancellationOrderId == null);

            return order.Status == OrderStatus.Cancelled
                   && account.ReservedBalance.Amount == 0
                   && cancelSettled;
        }, failureMessage: "Cancellation was not settled and reserved funds were not released.");
    }

    private async Task<string> RegisterAndFundAsync(string email)
    {
        const string password = "Password123!";
        var registerResponse = await _client.PostAsJsonAsync(
            "/api/accounts/register",
            new RegisterUserCommand(email, password, "Test", "User"));
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/accounts/login",
            new LoginCommand(email, password));
        loginResponse.EnsureSuccessStatusCode();

        var login = await loginResponse.Content
            .ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
        var account = await db.UserAccounts.FindAsync(login!.Data!.UserId);
        account!.Deposit(new Money(100_000m, Currency.USD));
        await db.SaveChangesAsync();

        return login.Data.Token;
    }
}