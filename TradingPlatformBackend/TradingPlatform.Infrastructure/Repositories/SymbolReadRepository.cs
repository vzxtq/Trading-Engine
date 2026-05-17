using Microsoft.EntityFrameworkCore;
using TradingEngine.Application.Interfaces.Symbols;
using TradingEngine.Infrastructure.Persistence;
using TradingEngine.Domain.Entities;

namespace TradingEngine.Infrastructure.Repositories
{
    public class SymbolReadRepository : ISymbolReadRepository
    {
        private readonly TradingDbContext _dbContext;

        public SymbolReadRepository(TradingDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<List<string>> GetAllSymbolsAsync()
        {
            return await _dbContext.Symbols
                .AsNoTracking()
                .Select(s => s.Name)
                .ToListAsync();
        }

        public async Task<SymbolDomain?> GetSymbolByNameAsync(string name, CancellationToken cancellationToken)
        {
            return await _dbContext.Symbols
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Name == name, cancellationToken);
        }
    }
}
