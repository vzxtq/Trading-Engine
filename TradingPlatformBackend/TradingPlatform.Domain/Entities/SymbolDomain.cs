using TradingEngine.Domain.Enums;

namespace TradingEngine.Domain.Entities
{
    // Temporary directory of tradeable symbols until external API integration
    public class SymbolDomain
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public Currency Currency { get; private set; }

        private SymbolDomain() { }

        public static SymbolDomain Create(string name, Currency currency)
        {
            return new SymbolDomain
            {
                Id = Guid.NewGuid(),
                Name = name,
                Currency = currency
            };
        }
    }
}