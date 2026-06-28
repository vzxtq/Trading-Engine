namespace TradingEngine.Domain.ValueObjects;

public record OrderSummary(
    int TotalOrders,
    int OpenOrders,
    int FilledOrders,
    int CancelledOrders,
    decimal TotalVolume)
{
    private const double EmptyRate = 0;
    private const double PercentageMultiplier = 100;

    public double FillRate => TotalOrders == 0 ? EmptyRate : (double)FilledOrders / TotalOrders * PercentageMultiplier;
    public double CancelledRate => TotalOrders == 0 ? EmptyRate : (double)CancelledOrders / TotalOrders * PercentageMultiplier;
}
