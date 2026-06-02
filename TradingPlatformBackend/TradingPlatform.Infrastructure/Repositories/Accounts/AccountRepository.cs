using Microsoft.EntityFrameworkCore;
using TradingEngine.Application.Interfaces.Accounts;
using TradingEngine.Infrastructure.Persistence;
using TradingEngine.Domain.Entities;

namespace TradingEngine.Infrastructure.Repositories.Accounts;

public class AccountRepository : IAccountRepository
{
    private readonly TradingDbContext _dbContext;

    public AccountRepository(TradingDbContext context)
    {
        _dbContext = context;
    }

    public async Task AddAsync(
        UserAccountDomain account,
        CancellationToken cancellationToken)
    {
        await _dbContext.UserAccounts.AddAsync(account, cancellationToken);
    }

    public async Task<UserAccountDomain?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await _dbContext.UserAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task UpdateAsync(
        UserAccountDomain account,
        CancellationToken cancellationToken)
    {
        _dbContext.UserAccounts.Update(account);
        return Task.CompletedTask;
    }
}
