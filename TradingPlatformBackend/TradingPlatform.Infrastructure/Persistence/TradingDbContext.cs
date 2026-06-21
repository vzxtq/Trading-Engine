using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingEngine.Domain.Common;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.Events;
using TradingEngine.Infrastructure.Persistence.Configurations;
using TradingEngine.Infrastructure.Persistence.Outbox;

namespace TradingEngine.Infrastructure.Persistence
{
    public class TradingDbContext : DbContext
    {
        private readonly IMediator? _mediator;

        public TradingDbContext(
            DbContextOptions<TradingDbContext> options,
            IMediator? mediator = null)
            : base(options)
        {
            _mediator = mediator;
        }

        public TradingDbContext(DbContextOptions<TradingDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserAccountDomain> UserAccounts => Set<UserAccountDomain>();
        public DbSet<UserIdentityDomain> UserIdentities => Set<UserIdentityDomain>();
        public DbSet<OrderDomain> Orders => Set<OrderDomain>();
        public DbSet<TradeDomain> Trades => Set<TradeDomain>();
        public DbSet<PositionDomain> Positions => Set<PositionDomain>();
        public DbSet<SymbolDomain> Symbols => Set<SymbolDomain>();
        public DbSet<OrderCommandOutboxEntry> OrderCommandOutbox => Set<OrderCommandOutboxEntry>();
        public DbSet<ExecutionResultOutboxEntry> ExecutionResultOutbox => Set<ExecutionResultOutboxEntry>();
        public DbSet<ProcessedExecutionReceipt> ProcessedExecutionReceipts => Set<ProcessedExecutionReceipt>();
        public DbSet<SymbolCommandSequence> SymbolCommandSequences => Set<SymbolCommandSequence>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Ignore<DomainEvent>();

            modelBuilder.ApplyConfiguration(new UserAccountConfiguration());
            modelBuilder.ApplyConfiguration(new UserIdentityConfiguration());
            modelBuilder.ApplyConfiguration(new OrderConfiguration());
            modelBuilder.ApplyConfiguration(new TradeConfiguration());
            modelBuilder.ApplyConfiguration(new PositionConfiguration());
            modelBuilder.ApplyConfiguration(new SymbolConfiguration());
            modelBuilder.ApplyConfiguration(new OrderCommandOutboxConfiguration());
            modelBuilder.ApplyConfiguration(new ExecutionResultOutboxConfiguration());
            modelBuilder.ApplyConfiguration(new ProcessedExecutionReceiptConfiguration());
            modelBuilder.ApplyConfiguration(new SymbolCommandSequenceConfiguration());
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entitiesWithEvents = ChangeTracker.Entries<AggregateRoot>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();

            var domainEvents = entitiesWithEvents
                .SelectMany(e => e.DomainEvents)
                .ToList();

            var result = await base.SaveChangesAsync(cancellationToken);

            foreach (var entity in entitiesWithEvents)
            {
                entity.ClearDomainEvents();
            }

            foreach (var domainEvent in domainEvents)
            {
                if (_mediator != null)
                {
                    var wrapperType = typeof(DomainEventNotification<>)
                        .MakeGenericType(domainEvent.GetType());

                    var wrapper = Activator.CreateInstance(wrapperType, domainEvent);

                    if (wrapper != null)
                    {
                        await _mediator.Publish(wrapper, cancellationToken);
                    }
                }
            }

            return result;
        }
    }
}
