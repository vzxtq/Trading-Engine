using TradingEngine.Domain.Enums;

namespace TradingEngine.Application.Features.Orders.Dtos;

/// <summary>
/// DTO for transferring order information.
/// </summary>
public class OrderDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string SymbolName { get; set; } = string.Empty;
    public Currency Currency { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public OrderSide Side { get; set; }
    public OrderStatus Status { get; set; }
    public long CreatedAt { get; set; }
    public long? UpdatedAt { get; set; }
}
