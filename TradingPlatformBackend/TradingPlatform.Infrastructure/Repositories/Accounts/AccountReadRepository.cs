using Microsoft.EntityFrameworkCore;
using TradingEngine.Application.Features.Accounts.Dtos;
using TradingEngine.Application.Interfaces.Accounts;
using TradingEngine.Infrastructure.Persistence;

namespace TradingEngine.Infrastructure.Repositories.Accounts
{
    public class AccountReadRepository : IAccountReadRepository
    {
        private readonly TradingDbContext _dbContext;

        public AccountReadRepository(TradingDbContext context)
        {
            _dbContext = context;
        }

        public async Task<AccountViewDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var account = await _dbContext.UserAccounts.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (account == null)
                return null;

            return new AccountViewDto
            {
                Id = account.Id,
                Email = account.Email,
                FullName = account.FullName,
                FirstName = account.FirstName,
                LastName = account.LastName,
                Balance = new MoneyDto
                {
                    Amount = account.Balance.Amount,
                    Currency = account.Balance.Currency,
                },
                ReservedBalance = new MoneyDto
                {
                    Amount = account.ReservedBalance.Amount,
                    Currency = account.ReservedBalance.Currency,
                },
                AvailableBalance = new MoneyDto
                {
                    Amount = account.AvailableBalance.Amount,
                    Currency = account.AvailableBalance.Currency,
                },
                LastLoginAt = account.LastLoginAt,
                IsActive = account.IsActive,
                CreatedAt = account.CreatedAt
            };
        }
    }
}
