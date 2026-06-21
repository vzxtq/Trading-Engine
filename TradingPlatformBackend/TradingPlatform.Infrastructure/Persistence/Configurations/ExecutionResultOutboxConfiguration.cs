using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingEngine.Infrastructure.Persistence.Outbox;

namespace TradingEngine.Infrastructure.Persistence.Configurations;

public sealed class ExecutionResultOutboxConfiguration : IEntityTypeConfiguration<ExecutionResultOutboxEntry>
{
    public void Configure(EntityTypeBuilder<ExecutionResultOutboxEntry> builder)
    {
        builder.ToTable("ExecutionResultOutbox");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.CommandOutboxId).IsUnique();
        builder.HasIndex(x => new { x.SymbolId, x.SequenceId }).IsUnique();
        builder.HasIndex(x => new { x.Status, x.CreatedAt });

        builder.Property(x => x.Status).HasConversion<string>();
        builder.Property(x => x.ResultType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Payload).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(2000);
        builder.HasOne<OrderCommandOutboxEntry>()
            .WithOne()
            .HasForeignKey<ExecutionResultOutboxEntry>(x => x.CommandOutboxId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion()
            .IsRequired();
    }
}
