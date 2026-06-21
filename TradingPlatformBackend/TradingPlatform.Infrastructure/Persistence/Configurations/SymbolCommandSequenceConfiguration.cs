using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingEngine.Infrastructure.Persistence.Outbox;

namespace TradingEngine.Infrastructure.Persistence.Configurations;

public sealed class SymbolCommandSequenceConfiguration
    : IEntityTypeConfiguration<SymbolCommandSequence>
{
    public void Configure(EntityTypeBuilder<SymbolCommandSequence> builder)
    {
        builder.ToTable("SymbolCommandSequences");
        builder.HasKey(x => x.SymbolId);
        builder.Property<byte[]>("RowVersion")
            .IsRowVersion()
            .IsRequired();
    }
}
