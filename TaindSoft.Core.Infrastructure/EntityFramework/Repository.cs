using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using TaindSoft.Core.Domain.Entities;
using TaindSoft.Core.Domain.Repositories;
using TaindSoft.Core.Domain.SoftDelete;

namespace TaindSoft.Core.Infrastructure.EntityFramework
{
    /// <summary>
    /// Base repository implementation for all aggregate roots
    /// </summary>
    /// <summary>
    /// Generic repository implementation for EF Core-backed aggregates and entities.
    /// </summary>
    public abstract class Repository<TDbContext, T> : IRepository<T>
        where TDbContext : BaseDbContext
        where T : Entity
    {
        protected readonly TDbContext _dbContext;
        protected readonly DbSet<T> _dbSet;
        protected readonly ILogger<Repository<TDbContext, T>> _logger;

        public Repository(TDbContext dbContext, ILogger<Repository<TDbContext, T>>? logger = null)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _dbSet = _dbContext.Set<T>();
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<Repository<TDbContext, T>>.Instance;
        }

        public virtual IQueryable<T> GetQueryable()
        {
            return ApplySoftDeleteFilterIfNeeded(_dbSet);
        }

        // Queryable access (readers can call AsNoTracking if they need read-only behavior)

        public virtual Task<T?> GetByGuidAsync(Guid guid, CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = ApplySoftDeleteFilterIfNeeded(_dbSet);
            return query.FirstOrDefaultAsync(e => e.Guid == guid, cancellationToken);
        }

        // Compatibility shim for legacy calls
        public virtual Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = ApplySoftDeleteFilterIfNeeded(_dbSet);
            return query.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }

        public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = ApplySoftDeleteFilterIfNeeded(_dbSet);
            return await query.ToListAsync(cancellationToken);
        }

        public virtual async Task<IReadOnlyList<T>> GetAsync(
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            string? includeString = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = ApplySoftDeleteFilterIfNeeded(_dbSet).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(includeString))
            {
                query = query.Include(includeString);
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return orderBy != null ? await orderBy(query).ToListAsync(cancellationToken)
                : await query.ToListAsync(cancellationToken);
        }

        public virtual Task<(IReadOnlyList<T> Items, int Total, int Page, int PageSize)> GetPagedAsync(Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, int page = 1, int pageSize = 20, string? includeString = null, CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = ApplySoftDeleteFilterIfNeeded(_dbSet);

            query = query.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(includeString))
            {
                query = query.Include(includeString);
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }
            int total = query.Count();
            if (orderBy != null)
            {
                query = orderBy(query);
            }
            query = query.Skip((page - 1) * pageSize).Take(pageSize);
            return Task.FromResult<(IReadOnlyList<T> Items, int Total, int Page, int PageSize)>((query.ToList(), total, page, pageSize));
        }

        // IQueryRepository: paged read-only
        public virtual Task<(IReadOnlyList<T> Items, int Total, int Page, int PageSize)> GetPagedReadOnlyAsync(Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, int page = 1, int pageSize = 20, string? includeString = null, CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = ApplySoftDeleteFilterIfNeeded(_dbSet).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(includeString))
            {
                query = query.Include(includeString);
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            int total = query.Count();
            if (orderBy != null)
            {
                query = orderBy(query);
            }

            query = query.Skip((page - 1) * pageSize).Take(pageSize);
            return Task.FromResult<(IReadOnlyList<T> Items, int Total, int Page, int PageSize)>((query.ToList(), total, page, pageSize));
        }
        public virtual async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Diagnostic: dump ChangeTracker state before SaveChanges
            try
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    foreach (EntityEntry e in _dbContext.ChangeTracker.Entries())
                    {
                        IEnumerable<string> modified = e.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name);
                        string pkInfo = string.Join(", ", e.Properties
                            .Where(p => p.Metadata.IsPrimaryKey())
                            .Select(pk => $"{pk.Metadata.Name}: {pk.CurrentValue}"));
                        _logger.LogDebug("Tracked entity Type={Type} State={State} PK=[{PK}] Modified=[{Modified}]",
                            e.Entity.GetType().FullName, e.State, pkInfo, string.Join(",", modified));
                    }
                }

                _ = await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict detected. {Entries}", ex.Entries.Count);
                foreach (EntityEntry entry in ex.Entries)
                {
                    string pkInfo = string.Join(", ", entry.Properties
                        .Where(p => p.Metadata.IsPrimaryKey())
                        .Select(pk => $"{pk.Metadata.Name}:{pk.CurrentValue}"));
                    _logger.LogWarning("Conflicted entity Type={Type} State={State} PK=[{PK}]",
                        entry.Entity.GetType().FullName, entry.State, pkInfo);

                    string modifiedProps = string.Join(", ", entry.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name));
                    string originalValues = string.Join(", ", entry.Properties.Select(p => $"{p.Metadata.Name}:{entry.OriginalValues[p.Metadata.Name]}"));
                    string currentValues = string.Join(", ", entry.Properties.Select(p => $"{p.Metadata.Name}:{p.CurrentValue}"));

                    _logger.LogWarning("Conflicted entity detail: Modified=[{Modified}] Original=[{Original}] Current=[{Current}]",
                        modifiedProps, originalValues, currentValues);

                    PropertyValues? dbValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                    if (dbValues == null)
                    {
                        _logger.LogWarning("Database row missing (deleted by another transaction)");
                    }
                    else
                    {
                        string dbValuesStr = string.Join(", ", dbValues.Properties.Select(p => $"{p.Name}:{dbValues[p]}"));
                        _logger.LogWarning("Database values: [{DbValues}]", dbValuesStr);
                    }
                }

                throw;
            }
        }

        public virtual Task<int> GetCountAsync(
            Expression<Func<T, bool>>? predicate = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = ApplySoftDeleteFilterIfNeeded(_dbSet);

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return query.CountAsync(cancellationToken);
        }

        public virtual async Task<T> InsertAsync(T entity, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            EntityEntry<T> newEntity = await _dbSet.AddAsync(entity, cancellationToken);
            if (autoSave)
            {
                _ = await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return newEntity.Entity;
        }

        public virtual async Task InsertRangeAsync(IEnumerable<T> entities, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddRangeAsync(entities, cancellationToken);
            if (autoSave)
            {
                _ = await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        // Detached update APIs removed. For tracked aggregates, mutate and call SaveChangesAsync.

        // UpdateRangeAsync removed.

        public virtual async Task DeleteAsync(T entity, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            if (entity is ISoftDelete)
            {
                EntityEntry<T> entry = _dbContext.Entry(entity);
                entry.Property(nameof(ISoftDelete.DeletedAt)).CurrentValue = DateTime.UtcNow;
                entry.Property(nameof(ISoftDelete.DeletedBy)).CurrentValue = 0;
                entry.State = EntityState.Modified;
            }
            else
            {
                _ = _dbSet.Remove(entity);
            }

            if (autoSave)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        public virtual async Task DeleteRangeAsync(IEnumerable<T> entities, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            _dbSet.RemoveRange(entities);
            if (autoSave)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        public virtual async Task<bool> ExistsAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = ApplySoftDeleteFilterIfNeeded(_dbSet);
            return await query.AnyAsync(predicate, cancellationToken);
        }

        private static IQueryable<T> ApplySoftDeleteFilterIfNeeded(IQueryable<T> query)
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
            {
                return query;
            }

            // Filter by DeletedAt == null instead of IsDeleted (which is computed and not translatable)
            ParameterExpression parameter = Expression.Parameter(typeof(T), "entity");
            MemberExpression deletedAt = Expression.Property(parameter, nameof(ISoftDelete.DeletedAt));
            BinaryExpression notDeleted = Expression.Equal(deletedAt, Expression.Constant(null, typeof(DateTime?)));
            Expression<Func<T, bool>> predicate = Expression.Lambda<Func<T, bool>>(notDeleted, parameter);

            return query.Where(predicate);
        }
    }
}
