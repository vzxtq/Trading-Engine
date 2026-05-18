using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.ValueObjects;

namespace TradingEngine.Infrastructure.Persistence.Configurations;

public sealed class TradeConfiguration : IEntityTypeConfiguration<TradeDomain>
{
    public void Configure(EntityTypeBuilder<TradeDomain> builder)
    {
        builder.ToTable("Trades");

        builder.HasKey(t => t.Id)
            .HasName("PK_Trades");

        builder.HasIndex(t => t.ExecutedAt)
            .HasDatabaseName("IX_Trades_ExecutedAt");

        builder.Property(t => t.BuyOrderId).IsRequired();
        builder.Property(t => t.SellOrderId).IsRequired();
        builder.Property(t => t.BuyerId).IsRequired();
        builder.Property(t => t.SellerId).IsRequired();

        builder.Property(t => t.SymbolId)
            .IsRequired();

        builder.HasOne(t => t.Symbol)
            .WithMany()
            .HasForeignKey(t => t.SymbolId);

        builder.Property(t => t.Price)
               .HasConversion(v => v.Value, v => new Price(v))
               .HasColumnName("Price")
               .HasPrecision(18, 8)
               .IsRequired();

        builder.Property(t => t.Quantity)
               .HasConversion(v => v.Value, v => new Quantity(v))
               .HasColumnName("Quantity")
               .HasPrecision(18, 8)
               .IsRequired();

        builder.Property(t => t.ExecutedAt)
               .HasColumnName("ExecutedAt")
               .IsRequired();

        builder.Property(t => t.CreatedAt)
               .HasColumnName("CreatedAt")
               .HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
