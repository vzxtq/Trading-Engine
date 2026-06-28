namespace TradingEngine.Application.Features.Orders.Dtos;

public record OrderSummaryDto(
    int TotalOrders,
    int OpenOrders,
    int FilledOrders,
    int CancelledOrders,
    decimal TotalVolume,
    double FillRate,
    double CancelledRate);
