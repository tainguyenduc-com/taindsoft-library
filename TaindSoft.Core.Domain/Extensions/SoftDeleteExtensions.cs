using System.Linq.Expressions;
using TaindSoft.Core.Domain.Entities;
using TaindSoft.Core.Domain.Repositories;
using TaindSoft.Core.Domain.SoftDelete;

namespace TaindSoft.Core.Domain.Extensions
{
    /// <summary>
    /// Optional extension methods for soft-delete filtering.
    /// </summary>
    public static class SoftDeleteExtensions
    {
        public static IQueryable<T> WhereNotDeleted<T>(this IQueryable<T> query)
            where T : ISoftDelete
        {
            return query.Where(entity => !entity.IsDeleted);
        }

        public static IQueryable<T> WhereDeleted<T>(this IQueryable<T> query)
            where T : ISoftDelete
        {
            return query.Where(entity => entity.IsDeleted);
        }

        public static IQueryable<T> WhereNotDeleted<T>(this IRepository<T> repository)
            where T : Entity, ISoftDelete
        {
            return repository.GetQueryable().WhereNotDeleted();
        }

        public static IQueryable<T> WhereDeleted<T>(this IRepository<T> repository)
            where T : Entity, ISoftDelete
        {
            return repository.GetQueryable().WhereDeleted();
        }

        public static Expression<Func<T, bool>> IsNotDeletedPredicate<T>()
            where T : ISoftDelete
        {
            return entity => !entity.IsDeleted;
        }
    }
}
