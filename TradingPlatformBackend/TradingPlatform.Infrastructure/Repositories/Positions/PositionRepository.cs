using Microsoft.EntityFrameworkCore;
using TradingEngine.Application.Interfaces.Positions;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.ValueObjects;
using TradingEngine.Infrastructure.Persistence;

namespace TradingEngine.Infrastructure.Repositories.Positions;

public sealed class PositionRepository : IPositionRepository
{
    private readonly TradingDbContext _dbContext;

    public PositionRepository(TradingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PositionDomain?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Positions.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<PositionDomain?> GetUserPositionForSymbolAsync(Guid userId, string symbol, CancellationToken cancellationToken)
    {
        var symbolObj = new Symbol(symbol);
        return await _dbContext.Positions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.SymbolValue == symbolObj, cancellationToken);
    }

    public Task UpdateAsync(PositionDomain position, CancellationToken cancellationToken)
    {
        _dbContext.Positions.Update(position);
        return Task.CompletedTask;
    }

    public async Task AddAsync(PositionDomain position, CancellationToken cancellationToken)
    {
        await _dbContext.Positions.AddAsync(position, cancellationToken);
    }
}
