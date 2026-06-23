using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.ValueObjects;

namespace TradingEngine.Infrastructure.Persistence.Configurations;

public sealed class PositionConfiguration : IEntityTypeConfiguration<PositionDomain>
{
    public void Configure(EntityTypeBuilder<PositionDomain> builder)
    {
        builder.ToTable("Positions");

        builder.HasKey(p => p.Id)
               .HasName("PK_Positions");

        builder.Property(p => p.UserId).IsRequired();

        builder.Property(p => p.SymbolValue)
               .HasConversion(
                   v => v.Value,
                   v => new Symbol(v))
               .HasColumnName("Symbol")
               .HasMaxLength(10)
               .IsRequired();

        builder.HasIndex(p => new { p.UserId, p.SymbolValue })
               .IsUnique()
               .HasDatabaseName("UX_Positions_User_Symbol");

        builder.Property(p => p.Quantity)
               .HasConversion(v => v.Value, v => new Quantity(v))
               .HasColumnName("Quantity")
               .HasPrecision(18, 8)
               .IsRequired();

        builder.Property(p => p.ReservedQuantity)
               .HasConversion(v => v.Value, v => new Quantity(v))
               .HasColumnName("ReservedQuantity")
               .HasPrecision(18, 8)
               .IsRequired();

        builder.Property(p => p.AverageCost)
               .HasPrecision(18, 8)
               .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(p => p.UpdatedAt);
        builder.Property<byte[]>("RowVersion")
            .IsRowVersion()
            .IsRequired();
    }
}
