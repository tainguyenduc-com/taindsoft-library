using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TaindSoft.Core.Domain.Dispatchers;
using TaindSoft.Core.Domain.Entities;
using TaindSoft.Core.Domain.Events;
using TaindSoft.Core.Domain.ValueObjects;

namespace TaindSoft.Core.Infrastructure.EntityFramework
{
    /// <summary>
    /// Base DbContext with common configurations for all modules.
    /// Part of Core.Infrastructure skeleton - no dependencies on utility libraries.
    /// Supports domain event dispatching with outbox pattern.
    /// </summary>
    /// <remarks>
    /// Constructor with optional time provider and domain event dispatcher.
    /// </remarks>
    public abstract class BaseDbContext(
        DbContextOptions options,
        IDomainEventDispatcher? eventDispatcher = null) : DbContext(options)
    {
        private readonly IDomainEventDispatcher? _eventDispatcher = eventDispatcher;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure shadow properties for audit tracking
            ConfigureAuditingEntities(modelBuilder);

            // Apply module-specific configurations FIRST (owned types must be configured before entity iteration)
            ApplyModuleConfigurations(modelBuilder);

            // Configure all entity types
            IEnumerable<Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType> entityTypes = modelBuilder.Model.GetEntityTypes();

            foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType in entityTypes)
            {
                Type entity = entityType.ClrType;

                // Configure value objects (no table) – skip if already owned
                if (typeof(ValueObject).IsAssignableFrom(entity) &&
                    !typeof(IAggregateRootEntity).IsAssignableFrom(entity) &&
                    !(modelBuilder.Model.FindEntityType(entity)?.IsOwned() ?? false))
                {
                    _ = modelBuilder.Entity(entity).HasNoKey();
                }

                // If the entity inherits from Entity (our base), add a unique index on Guid
                if (typeof(Entity).IsAssignableFrom(entity) && entity != typeof(Entity))
                {
                    _ = modelBuilder.Entity(entity).HasIndex("Guid").IsUnique();
                }
            }
        }

        protected virtual void ConfigureAuditingEntities(ModelBuilder modelBuilder)
        {
            // Configure CreatedAt and UpdatedAt shadow properties
            foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(IAggregateRootEntity).IsAssignableFrom(entityType.ClrType))
                {
                    Microsoft.EntityFrameworkCore.Metadata.IMutableProperty? createdProp = entityType.FindProperty("CreatedAt");
                    if (createdProp == null)
                    {
                        _ = entityType.AddProperty("CreatedAt", typeof(DateTime));
                    }

                    Microsoft.EntityFrameworkCore.Metadata.IMutableProperty? updatedProp = entityType.FindProperty("UpdatedAt");
                    if (updatedProp == null)
                    {
                        _ = entityType.AddProperty("UpdatedAt", typeof(DateTime?));
                    }
                }
            }
        }

        /// <summary>
        /// Apply module-specific entity configurations.
        /// Default: scan the concrete DbContext's assembly for IEntityTypeConfiguration&lt;T&gt;.
        /// Each DbContext owns exactly one assembly — no cross-module duplication.
        /// </summary>
        protected virtual void ApplyModuleConfigurations(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);
        }

        /// <summary>
        /// Save changes with audit trail and domain event dispatching.
        /// Domain events are dispatched BEFORE commit (in-memory handlers).
        /// Domain events are published to outbox AFTER commit (eventual delivery).
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditTrail();

            // Collect domain events from aggregates BEFORE commit
            List<IDomainEvent> domainEvents = [.. ChangeTracker.Entries<IAggregateRootEntity>()
                .Where(e => e.Entity.DomainEvents.Count != 0)
                .SelectMany(e => e.Entity.DomainEvents)];

            // Clear domain events from aggregates to prevent re-processing
            foreach (EntityEntry<IAggregateRootEntity> entry in ChangeTracker.Entries<IAggregateRootEntity>())
            {
                entry.Entity.ClearDomainEvents();
            }

            // Dispatch to in-memory handlers BEFORE commit
            if (domainEvents.Count != 0 && _eventDispatcher != null)
            {
                await _eventDispatcher.DispatchAsync(domainEvents, cancellationToken);
            }

            // Commit transaction
            int result = await base.SaveChangesAsync(cancellationToken);

            // Publish to outbox AFTER commit (fire-and-forget, no throw)
            if (domainEvents.Count != 0 && _eventDispatcher != null)
            {
                await _eventDispatcher.PublishToOutboxAsync(domainEvents, cancellationToken);
            }

            return result;
        }

        public override int SaveChanges()
        {
            UpdateAuditTrail();

            // Synchronous version - collect and dispatch domain events
            List<IDomainEvent> domainEvents = [.. ChangeTracker.Entries<IAggregateRootEntity>()
                .Where(e => e.Entity.DomainEvents.Count != 0)
                .SelectMany(e => e.Entity.DomainEvents)];

            foreach (EntityEntry<IAggregateRootEntity> entry in ChangeTracker.Entries<IAggregateRootEntity>())
            {
                entry.Entity.ClearDomainEvents();
            }

            // Dispatch synchronously (blocking)
            if (domainEvents.Count != 0 && _eventDispatcher != null)
            {
                _eventDispatcher.DispatchAsync(domainEvents, default).GetAwaiter().GetResult();
            }

            int result = base.SaveChanges();

            // Publish to outbox after commit
            if (domainEvents.Count != 0 && _eventDispatcher != null)
            {
                _eventDispatcher.PublishToOutboxAsync(domainEvents, default).GetAwaiter().GetResult();
            }

            return result;
        }

        /// <summary>
        /// Update audit trail for aggregate roots
        /// </summary>
        private void UpdateAuditTrail()
        {
            IEnumerable<EntityEntry<IAggregateRootEntity>> entries = ChangeTracker.Entries<IAggregateRootEntity>();
            DateTime utcNow = DateTime.UtcNow;

            foreach (EntityEntry<IAggregateRootEntity> entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    // Only set CreatedAt if not already explicitly provided
                    if (entry.Property("CreatedAt").CurrentValue == null ||
                        (DateTime)(entry.Property("CreatedAt").CurrentValue ?? DateTime.MinValue) == default)
                    {
                        entry.Property("CreatedAt").CurrentValue = utcNow;
                    }
                    entry.Property("UpdatedAt").CurrentValue = utcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Property("UpdatedAt").CurrentValue = utcNow;
                }
            }
        }
    }
}
