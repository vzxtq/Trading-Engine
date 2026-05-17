using Microsoft.EntityFrameworkCore;
using TradingEngine.Application.Common;
using TradingEngine.Application.Features.Positions.Dtos;
using TradingEngine.Application.Interfaces.Positions;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.ValueObjects;
using TradingEngine.Infrastructure.Persistence;

namespace TradingEngine.Infrastructure.Repositories.Positions;

public sealed class PositionReadRepository : IPositionReadRepository
{
    private readonly TradingDbContext _dbContext;

    public PositionReadRepository(TradingDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<PositionDto>> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        var positions = await _dbContext.Positions
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToListAsync(ct);

        return positions.Select(p => MapToDto(p)).ToList();
    }

    public async Task<PositionDto?> GetUserPositionForSymbolAsync(Guid userId, string symbol, CancellationToken ct)
    {
        var symbolObj = new Symbol(symbol);
        var position = await _dbContext.Positions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.SymbolValue == symbolObj, ct);

        return position is null ? null : MapToDto(position);
    }

    private static PositionDto MapToDto(PositionDomain p)
    {
        return new PositionDto
        {
            Symbol = p.SymbolValue.Value,
            Quantity = p.Quantity.Value,
            AveragePrice = p.AverageCost,
            UnrealizedPnL = 0m,
            LastUpdated = (p.UpdatedAt ?? p.CreatedAt).ToUnixTimeMs()
        };
    }
}
