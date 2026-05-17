using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.ValueObjects;

namespace TradingEngine.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<OrderDomain>
{
    public void Configure(EntityTypeBuilder<OrderDomain> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id)
            .HasName("PK_Orders");

        builder.HasIndex(o => new { o.UserId, o.CreatedAt })
            .HasDatabaseName("IX_Orders_User_CreatedAt");

        builder.Property(o => o.UserId)
            .IsRequired();

        builder.Property(o => o.Side)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(o => o.SymbolId)
            .IsRequired();

        builder.HasOne(o => o.Symbol)
            .WithMany()
            .HasForeignKey(o => o.SymbolId);

        // Converters and Comparers for Value Objects
        var priceConverter = new ValueConverter<Price, decimal>(
            v => v.Value,
            v => new Price(v));
        var priceComparer = new ValueComparer<Price>(
            (l, r) => l.Value == r.Value,
            p => p.Value.GetHashCode(),
            p => new Price(p.Value));

        var quantityConverter = new ValueConverter<Quantity, decimal>(
            v => v.Value,
            v => new Quantity(v));
        var quantityComparer = new ValueComparer<Quantity>(
            (l, r) => l.Value == r.Value,
            q => q.Value.GetHashCode(),
            q => new Quantity(q.Value));

        builder.Property(o => o.Price)
               .HasConversion(priceConverter)
               .Metadata.SetValueComparer(priceComparer);
        builder.Property(o => o.Price)
               .HasColumnName("Price")
               .HasPrecision(18, 8)
               .IsRequired();

        builder.Property(o => o.Quantity)
               .HasConversion(quantityConverter)
               .Metadata.SetValueComparer(quantityComparer);
        builder.Property(o => o.Quantity)
               .HasColumnName("Quantity")
               .HasPrecision(18, 8)
               .IsRequired();

        builder.Property(o => o.RemainingQuantity)
               .HasConversion(quantityConverter)
               .Metadata.SetValueComparer(quantityComparer);
        builder.Property(o => o.RemainingQuantity)
               .HasColumnName("RemainingQuantity")
               .HasPrecision(18, 8)
               .IsRequired();

        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.UpdatedAt);
    }
}
