using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingEngine.Infrastructure.Persistence.Outbox;

namespace TradingEngine.Infrastructure.Persistence.Configurations;

public sealed class ProcessedExecutionReceiptConfiguration : IEntityTypeConfiguration<ProcessedExecutionReceipt>
{
    public void Configure(EntityTypeBuilder<ProcessedExecutionReceipt> builder)
    {
        builder.ToTable("ProcessedExecutionReceipts");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.SymbolId, x.SequenceId }).IsUnique();
    }
}
