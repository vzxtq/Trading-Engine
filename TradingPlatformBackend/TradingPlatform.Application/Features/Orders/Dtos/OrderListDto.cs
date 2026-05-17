using TradingEngine.Application.Features.Accounts.Dtos;
using TradingEngine.Domain.Enums;

namespace TradingEngine.Application.Features.Orders.Dtos;

public class OrderListDto
{
    public Guid Id { get; set; }
    public string SymbolName { get; set; } = string.Empty;
    public Currency Currency { get; set; }
    public OrderSide Side { get; set; }
    public string Type { get; set; } = string.Empty;
    public MoneyDto Price { get; set; } = new();
    public decimal Quantity { get; set; }
    public decimal FilledQuantity { get; set; }
    public OrderStatus Status { get; set; }
    public decimal RemainingQuantity { get; set; }
    public long CreatedAt { get; set; }
    public Guid UserId { get; set; }
    public long? UpdatedAt { get; set; }
}
