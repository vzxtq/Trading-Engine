using System.Text.Json;
using TradingEngine.Domain.ValueObjects;
using TradingEngine.MatchingEngine.Commands;
using TradingEngine.MatchingEngine.Models;

namespace TradingEngine.Infrastructure.Persistence.Outbox;

internal static class OutboxSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyDictionary<Type, ExecutionResultRegistration>
        ExecutionResultsByType = CreateExecutionResultRegistrations();

    private static readonly IReadOnlyDictionary<string, ExecutionResultRegistration>
        ExecutionResultsByName = ExecutionResultsByType.Values.ToDictionary(
            registration => registration.TypeName,
            StringComparer.Ordinal);

    private static readonly IReadOnlyDictionary<
        OrderCommandType,
        Func<OrderCommandOutboxEntry, MatchingEngineCommand>>
        CommandFactories =
            new Dictionary<
                OrderCommandType,
                Func<OrderCommandOutboxEntry, MatchingEngineCommand>>
            {
                [OrderCommandType.AddOrder] = CreateAddOrderCommand,
                [OrderCommandType.CancelOrder] = CreateCancelOrderCommand
            };

    public static string SerializeExecutionResult(ExecutionResult result)
    {
        return GetExecutionResultRegistration(result).Serialize(result);
    }

    public static string GetExecutionResultType(ExecutionResult result)
    {
        return GetExecutionResultRegistration(result).TypeName;
    }

    public static ExecutionResult DeserializeExecutionResult(
        string resultType,
        string payload)
    {
        if (!ExecutionResultsByName.TryGetValue(resultType, out var registration))
        {
            throw new InvalidOperationException($"Unsupported execution result type {resultType}.");
        }

        return registration.Deserialize(payload);
    }

    public static MatchingEngineCommand DeserializeCommand(
        OrderCommandOutboxEntry entry)
    {
        if (!CommandFactories.TryGetValue(entry.CommandType, out var factory))
        {
            throw new InvalidOperationException($"Unsupported order command type {entry.CommandType}.");
        }

        return factory(entry);
    }

    private static IReadOnlyDictionary<Type, ExecutionResultRegistration>
        CreateExecutionResultRegistrations()
    {
        var registrations = new[]
        {
              RegisterExecutionResult<ExecutionResult.Accepted>(
                  nameof(ExecutionResult.Accepted),
                  "Accepted execution result payload is invalid."),

              RegisterExecutionResult<ExecutionResult.Rejected>(
                  nameof(ExecutionResult.Rejected),
                  "Rejected execution result payload is invalid.")
          };

        return registrations.ToDictionary(registration => registration.ResultType);
    }

    private static ExecutionResultRegistration RegisterExecutionResult<TResult>(
        string typeName,
        string invalidPayloadMessage)
        where TResult : ExecutionResult
    {
        return new ExecutionResultRegistration(
            typeof(TResult),
            typeName,
            result => JsonSerializer.Serialize((TResult)result, Options),
            payload => JsonSerializer.Deserialize<TResult>(payload, Options) ?? throw new InvalidOperationException(invalidPayloadMessage));
    }

    private static ExecutionResultRegistration GetExecutionResultRegistration(
        ExecutionResult result)
    {
        if (!ExecutionResultsByType.TryGetValue(result.GetType(), out var registration))
        {
            throw new InvalidOperationException(
                $"Unsupported execution result type {result.GetType().Name}.");
        }

        return registration;
    }

    private static AddOrderCommand CreateAddOrderCommand(
        OrderCommandOutboxEntry entry)
    {
        var payload = JsonSerializer.Deserialize<AddOrderCommandPayload>(
            entry.Payload, Options) ?? throw new InvalidOperationException("Add-order outbox payload is invalid.");

        return new AddOrderCommand
        {
            CommandOutboxId = entry.Id,
            SequenceId = GetSequenceId(entry),
            OrderId = payload.OrderId,
            UserId = payload.UserId,
            SymbolId = payload.SymbolId,
            Symbol = new Symbol(payload.Symbol),
            Price = payload.Price,
            Quantity = payload.Quantity,
            Side = payload.Side,
            Type = payload.Type,
            MaxTotalCost = payload.MaxTotalCost,
            ReceivedAt = ToUnixTimeMilliseconds(entry.CreatedAt)
        };
    }

    private static CancelOrderCommand CreateCancelOrderCommand(
        OrderCommandOutboxEntry entry)
    {
        var payload = JsonSerializer.Deserialize<CancelOrderCommandPayload>(
            entry.Payload, Options) ?? throw new InvalidOperationException("Cancel-order outbox payload is invalid.");

        return new CancelOrderCommand
        {
            CommandOutboxId = entry.Id,
            SequenceId = GetSequenceId(entry),
            OrderId = payload.OrderId,
            SymbolId = payload.SymbolId,
            Symbol = new Symbol(payload.Symbol),
            ReceivedAt = ToUnixTimeMilliseconds(entry.CreatedAt)
        };
    }

    private static long ToUnixTimeMilliseconds(DateTime value)
    {
        return new DateTimeOffset(
            DateTime.SpecifyKind(value, DateTimeKind.Utc))
            .ToUnixTimeMilliseconds();
    }

    private static long GetSequenceId(OrderCommandOutboxEntry entry)
    {
        return entry.SequenceId ?? throw new InvalidOperationException($"Order command {entry.Id} has no durable sequence.");
    }

    private sealed record ExecutionResultRegistration(
        Type ResultType,
        string TypeName,
        Func<ExecutionResult, string> Serialize,
        Func<string, ExecutionResult> Deserialize);
}
