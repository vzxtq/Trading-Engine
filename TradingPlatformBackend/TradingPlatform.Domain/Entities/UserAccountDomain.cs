using TradingEngine.Domain.Common;
using TradingEngine.Domain.ValueObjects;

namespace TradingEngine.Domain.Entities;

/// <summary>
/// Represents a user trading account.
/// Aggregate root responsible for balance management.
/// </summary>
public class UserAccountDomain : AggregateRoot
{
    public string Email { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
    public Money Balance { get; private set; }
    public Money ReservedBalance { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public bool IsActive { get; private set; } = false;

    private UserAccountDomain()
    {
        Email = null!;
        FirstName = null!;
        LastName = null!;
        Balance = null!;
        ReservedBalance = null!;
    }

    public UserAccountDomain(
        Guid id,
        string email,
        string firstName,
        string lastName,
        Money balance,
        Money reservedBalance)
    {
        Id = id;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Balance = balance;
        ReservedBalance = reservedBalance;
    }

    public static UserAccountDomain Create(string email, string firstName, string lastName, Money initialBalance)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty.", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty.", nameof(lastName));

        if (initialBalance.Amount < 0)
            throw new ArgumentException("Initial balance cannot be negative.", nameof(initialBalance));

        return new UserAccountDomain(
            Guid.NewGuid(),
            email.Trim().ToLowerInvariant(),
            firstName.Trim(),
            lastName.Trim(),
            initialBalance,
            Money.Zero(initialBalance.Currency))
        {
            IsActive = true
        };
    }

    /// <summary>
    /// Returns funds available for trading.
    /// </summary>
    public Money AvailableBalance => Balance - ReservedBalance;

    /// <summary>
    /// Deposits funds into the account.
    /// </summary>
    public void Deposit(Money amount)
    {
        EnsureActive();
        EnsurePositive(amount);

        Balance += amount;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Withdraws funds from the account.
    /// </summary>
    public void Withdraw(Money amount)
    {
        EnsureActive();
        EnsurePositive(amount);

        if (AvailableBalance < amount)
            throw new InvalidOperationException("Insufficient available balance.");

        Balance -= amount;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reserves funds for an order.
    /// </summary>
    public void ReserveFunds(Money amount)
    {
        EnsureActive();
        EnsurePositive(amount);

        if (AvailableBalance < amount)
            throw new InvalidOperationException("Insufficient funds for reservation.");

        ReservedBalance += amount;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Releases reserved funds when an order is cancelled.
    /// </summary>
    public void ReleaseReservedFunds(Money amount)
    {
        EnsurePositive(amount);

        if (ReservedBalance < amount)
            throw new InvalidOperationException("Cannot release more than reserved.");

        ReservedBalance -= amount;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Finalizes a trade and deducts reserved funds.
    /// </summary>
    public void CommitReservedFunds(Money amount)
    {
        EnsurePositive(amount);

        if (ReservedBalance < amount)
            throw new InvalidOperationException("Reserved balance is insufficient.");

        if (Balance < amount)
            throw new InvalidOperationException("Balance is insufficient.");

        ReservedBalance -= amount;
        Balance -= amount;

        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string firstName, string lastName, string email)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty", nameof(lastName));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        Email = email.Trim().ToLowerInvariant();

        FirstName = firstName.Trim();
        LastName = lastName.Trim();

        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // Account is online
    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    // Account is offline
    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureActive()
    {
        if (!IsActive)
            throw new InvalidOperationException("Account is inactive.");
    }

    private static void EnsurePositive(Money amount)
    {
        if (amount.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");
    }
}
