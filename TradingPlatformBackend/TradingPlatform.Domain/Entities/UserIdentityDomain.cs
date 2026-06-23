using TradingEngine.Domain.Common;

namespace TradingEngine.Domain.Entities;

public sealed class UserIdentityDomain : BaseEntity
{
    public Guid UserId { get; private set; }
    public string PasswordHash { get; private set; } = null!;
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiresAt { get; private set; }

    private UserIdentityDomain() { }

    public UserIdentityDomain(Guid userId, string passwordHash)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        PasswordHash = passwordHash;
    }

    public void UpdateRefreshToken(string token, DateTime expiry)
    {
        RefreshToken = token;
        RefreshTokenExpiresAt = expiry;
    }

    public void InvalidateRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiresAt = null;
    }

    public void UpdatePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty.");
        
        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
    }
}
