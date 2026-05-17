using TradingEngine.Domain.Entities;

namespace TradingEngine.Application.Interfaces.Symbols
{
    public interface ISymbolReadRepository
    {
        Task<List<string>> GetAllSymbolsAsync();
        Task<SymbolDomain?> GetSymbolByNameAsync(string name, CancellationToken cancellationToken);
    }
}
