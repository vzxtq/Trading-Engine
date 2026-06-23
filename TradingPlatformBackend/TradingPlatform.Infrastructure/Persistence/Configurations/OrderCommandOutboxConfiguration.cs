using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingEngine.Infrastructure.Persistence.Outbox;

namespace TradingEngine.Infrastructure.Persistence.Configurations;

public sealed class OrderCommandOutboxConfiguration : IEntityTypeConfiguration<OrderCommandOutboxEntry>
{
    public void Configure(EntityTypeBuilder<OrderCommandOutboxEntry> builder)
    {
        builder.ToTable("OrderCommandOutbox");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EnqueueId)
            .ValueGeneratedOnAdd();

        builder.HasIndex(x => x.EnqueueId)
            .IsUnique();

        builder.HasIndex(x => new { x.SymbolId, x.SequenceId })
            .IsUnique();

        builder.HasIndex(x => new { x.Status, x.EnqueueId });
        builder.HasIndex(x => new { x.OrderId, x.CommandType, x.Status });
        builder.HasIndex(x => x.ActiveCancellationOrderId).IsUnique();

        builder.Property(x => x.CommandType).HasConversion<string>();
        builder.Property(x => x.Status).HasConversion<string>();
        builder.Property(x => x.Payload).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(2000);
        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property<byte[]>("RowVersion")
            .IsRowVersion()
            .IsRequired();
    }
}
