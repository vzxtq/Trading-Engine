using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.Enums;

namespace TradingEngine.Infrastructure.Persistence.Configurations;

public class UserAccountConfiguration : IEntityTypeConfiguration<UserAccountDomain>
{
    public void Configure(EntityTypeBuilder<UserAccountDomain> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(x => x.Id)
               .HasName("PK_User_Accounts");

        builder.HasIndex(x => x.Email)
               .IsUnique()
               .HasDatabaseName("UX_User_Accounts_Email");

        builder.HasIndex(x => x.IsActive)
               .HasDatabaseName("IX_User_Accounts_Is_Active");

        builder.Property(x => x.Email)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(x => x.FirstName)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(x => x.LastName)
               .IsRequired()
               .HasMaxLength(100);

        builder.OwnsOne(x => x.Balance, balance =>
        {
            balance.Property(m => m.Amount)
                   .HasColumnName("BalanceAmount")
                   .HasPrecision(18, 8);
            balance.Property(m => m.Currency)
                   .HasColumnName("BalanceCurrency")
                   .HasConversion<string>();
        });

        builder.OwnsOne(x => x.ReservedBalance, reserved =>
        {
            reserved.Property(m => m.Amount)
                    .HasColumnName("ReservedBalanceAmount")
                    .HasPrecision(18, 8);
            reserved.Property(m => m.Currency)
                   .HasColumnName("ReservedBalanceCurrency")
                   .HasConversion<string>();
        });

        builder.Property(x => x.LastLoginAt)
               .HasColumnName("LastLoginAt");

        builder.Property(x => x.IsActive)
               .HasColumnName("IsActive");

        builder.Property(x => x.CreatedAt)
               .HasColumnName("CreatedAt")
               .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.UpdatedAt)
               .HasColumnName("UpdatedAt");

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion()
            .IsRequired();
    }
}
