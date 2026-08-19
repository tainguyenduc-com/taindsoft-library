using Microsoft.EntityFrameworkCore;
using TaindSoft.Core.Domain.Entities;
using TaindSoft.Core.Domain.Repositories;
using TaindSoft.Core.Domain.Specifications;

namespace TaindSoft.Core.Infrastructure.EntityFramework.Extensions
{
    /// <summary>
    /// Extension methods for IRepository to support specifications
    /// </summary>
    public static class SpecificationExtensions
    {
        /// <summary>
        /// Gets a single entity matching the specification
        /// </summary>
        /// <remarks>
        /// Returns the first entity matching the specification criteria.
        /// Throws if multiple entities match.
        /// </remarks>
        public static async Task<T?> FirstOrDefaultAsync<T>(
            this IRepository<T> repository,
            ISpecification<T> specification,
            CancellationToken cancellationToken = default)
            where T : Entity
        {
            ArgumentNullException.ThrowIfNull(specification);

            return await ApplySpecification(repository, specification)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Gets a list of entities matching the specification
        /// </summary>
        /// <remarks>
        /// Returns all entities matching the specification criteria, respecting
        /// filters, includes, ordering, and pagination.
        /// </remarks>
        public static async Task<List<T>> ListAsync<T>(
            this IRepository<T> repository,
            ISpecification<T> specification,
            CancellationToken cancellationToken = default)
            where T : Entity
        {
            ArgumentNullException.ThrowIfNull(specification);

            return await ApplySpecification(repository, specification)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Gets the count of entities matching the specification
        /// </summary>
        public static async Task<int> CountAsync<T>(
            this IRepository<T> repository,
            ISpecification<T> specification,
            CancellationToken cancellationToken = default)
            where T : Entity
        {
            ArgumentNullException.ThrowIfNull(specification);

            return await ApplySpecification(repository, specification, countQuery: true)
                .CountAsync(cancellationToken);
        }

        /// <summary>
        /// Gets whether any entity matches the specification
        /// </summary>
        public static async Task<bool> AnyAsync<T>(
            this IRepository<T> repository,
            ISpecification<T> specification,
            CancellationToken cancellationToken = default)
            where T : Entity
        {
            ArgumentNullException.ThrowIfNull(specification);

            return await ApplySpecification(repository, specification, countQuery: true)
                .AnyAsync(cancellationToken);
        }

        /// <summary>
        /// Applies the specification to the repository query
        /// </summary>
        private static IQueryable<T> ApplySpecification<T>(
            IRepository<T> repository,
            ISpecification<T> specification,
            bool countQuery = false)
            where T : Entity
        {
            IQueryable<T> query = repository.GetQueryable();

            // Apply criteria (WHERE)
            if (specification.Criteria != null)
            {
                query = query.Where(specification.Criteria);
            }

            // Apply includes (INCLUDE) - only for non-count queries
            if (!countQuery)
            {
                query = specification.Includes.Aggregate(
                    query,
                    (current, include) => current.Include(include));

                // Apply string-based includes
                query = specification.IncludeStrings.Aggregate(
                    query,
                    (current, include) => current.Include(include));
            }

            // Apply ordering
            if (specification.OrderBy != null)
            {
                query = query.OrderBy(specification.OrderBy);
            }
            else if (specification.OrderByDescending != null)
            {
                query = query.OrderByDescending(specification.OrderByDescending);
            }

            // Apply paging
            if (specification.IsPagingEnabled)
            {
                query = query.Skip(specification.Skip).Take(specification.Take);
            }

            // Apply tracking
            if (!specification.IsTrackingEnabled)
            {
                query = query.AsNoTracking();
            }

            return query;
        }
    }
}
