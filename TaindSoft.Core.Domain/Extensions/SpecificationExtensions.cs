using System.Linq.Expressions;
using TaindSoft.Core.Domain.Entities;
using TaindSoft.Core.Domain.Repositories;
using TaindSoft.Core.Domain.Specifications;

namespace TaindSoft.Core.Domain.Extensions
{
    /// <summary>
    /// Extension methods for IRepository to support specifications (EF-free implementations)
    /// NOTE: This implementation uses repository abstraction methods and intentionally
    /// avoids direct EF Core APIs so Domain project can be compiled without EF Core.
    /// Some features of ISpecification (expression-based Includes) may be ignored by
    /// this fallback and should be implemented in Infrastructure-specific evaluators.
    /// </summary>
    public static class SpecificationExtensions
    {
        public static async Task<T?> FirstOrDefaultAsync<T>(
            this IRepository<T> repository,
            ISpecification<T> specification,
            CancellationToken cancellationToken = default)
            where T : Entity
        {
            ArgumentNullException.ThrowIfNull(specification);

            // Use repository.GetAsync to fetch matching items and return first or default.
            var list = await repository.GetAsync(
                specification.Criteria,
                null,
                specification.IncludeStrings.FirstOrDefault(),
                cancellationToken);

            return list?.FirstOrDefault();
        }

        public static async Task<List<T>> ListAsync<T>(
            this IRepository<T> repository,
            ISpecification<T> specification,
            CancellationToken cancellationToken = default)
            where T : Entity
        {
            ArgumentNullException.ThrowIfNull(specification);

            var list = await repository.GetAsync(
                specification.Criteria,
                null,
                specification.IncludeStrings.FirstOrDefault(),
                cancellationToken);

            return list?.ToList() ?? new List<T>();
        }

        public static async Task<int> CountAsync<T>(
            this IRepository<T> repository,
            ISpecification<T> specification,
            CancellationToken cancellationToken = default)
            where T : Entity
        {
            ArgumentNullException.ThrowIfNull(specification);

            return await repository.GetCountAsync(specification.Criteria, cancellationToken);
        }

        public static async Task<bool> AnyAsync<T>(
            this IRepository<T> repository,
            ISpecification<T> specification,
            CancellationToken cancellationToken = default)
            where T : Entity
        {
            ArgumentNullException.ThrowIfNull(specification);

            Expression<Func<T, bool>>? pred = specification.Criteria; if (pred is null) pred = _ => true; return await repository.ExistsAsync(pred, cancellationToken);
        }
    }
}
