using System.Linq.Expressions;
using TaindSoft.Core.Domain.Entities;
namespace TaindSoft.Core.Domain.Repositories
{
    /// <summary>
    /// Base repository interface for all aggregate roots
    /// </summary>
    public interface IRepository<T> where T : Entity
    {
        IQueryable<T> GetQueryable();
        /// <summary>
        /// Get an aggregate root for command operations (tracked by the DbContext).
        /// Use this method in command handlers where you intend to mutate the aggregate and call SaveChangesAsync().
        /// </summary>
        Task<T?> GetByGuidAsync(Guid guid, CancellationToken cancellationToken = default);

        // Compatibility shim for legacy code
        Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<T>> GetAsync(
        Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            string? includeString = null,
            CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<T> Items, int Total, int Page, int PageSize)> GetPagedAsync(
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null
            , int page = DefaultConstants.PagedConstants.Page, int pageSize = DefaultConstants.PagedConstants.PageSize
            , string? includeString = null
            , CancellationToken cancellationToken = default);

        Task<int> GetCountAsync(
            Expression<Func<T, bool>>? predicate = null,
            CancellationToken cancellationToken = default);
        Task<T> InsertAsync(T entity, bool autoSave = true, CancellationToken cancellationToken = default);
        Task InsertRangeAsync(IEnumerable<T> entities, bool autoSave = true, CancellationToken cancellationToken = default);
        /// <summary>
        /// Update a detached entity. This method is intended ONLY for detached entity scenarios
        /// (e.g. background jobs, importers, or when entity was not loaded by the current DbContext).
        /// Do NOT use for aggregates loaded from the repository in the same command handler.
        /// For tracked aggregates, mutate the entity and call <see cref="SaveChangesAsync"/>.
        /// Calling this method for a tracked entity will throw at runtime.
        /// </summary>
        // Detached update APIs removed — for tracked aggregates, mutate and call SaveChangesAsync
        Task DeleteAsync(T entity, bool autoSave = true, CancellationToken cancellationToken = default);
        Task DeleteRangeAsync(IEnumerable<T> entities, bool autoSave = true, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
