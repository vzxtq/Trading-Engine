using TradingEngine.Domain.Enums;

namespace TradingEngine.Application.Features.Orders.Dtos
{
    public class OrderFilterDto
    {
        public OrderSide? Side { get; set; }
        public OrderStatus? Status { get; set; }
        public Guid? SymbolId { get; set; }
        public string? Search { get; set; }
    }
}
