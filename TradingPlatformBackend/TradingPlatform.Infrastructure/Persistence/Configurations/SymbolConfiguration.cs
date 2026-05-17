using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingEngine.Domain.Entities;

namespace TradingEngine.Infrastructure.Persistence.Configurations
{
    public sealed class SymbolConfiguration : IEntityTypeConfiguration<SymbolDomain>
    {
        public void Configure(EntityTypeBuilder<SymbolDomain> builder)
        {
            builder.ToTable("Symbols");

            builder.HasKey(s => s.Id)
                .HasName("PK_Symbols");

            builder.Property(s => s.Name)
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(s => s.Currency)
                .IsRequired()
                .HasConversion<string>();

            builder.HasIndex(s => s.Name)
                .IsUnique()
                .HasDatabaseName("IX_Symbols_Name");
        }
    }
}
