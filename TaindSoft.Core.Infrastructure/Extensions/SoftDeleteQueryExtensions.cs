using System.Linq.Expressions;
using TaindSoft.Core.Domain.SoftDelete;

namespace TaindSoft.Core.Infrastructure.Extensions
{
    /// <summary>
    /// Optional extension methods for soft-delete filtering.
    /// </summary>
    public static class SoftDeleteQueryExtensions
    {
        public static IQueryable<T> ApplySoftDeleteFilter<T>(this IQueryable<T> query)
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
            {
                return query;
            }

            ParameterExpression parameter = Expression.Parameter(typeof(T), "entity");
            UnaryExpression casted = Expression.Convert(parameter, typeof(ISoftDelete));
            MemberExpression isDeletedProperty = Expression.Property(casted, nameof(ISoftDelete.IsDeleted));
            UnaryExpression notDeleted = Expression.Not(isDeletedProperty);
            Expression<Func<T, bool>> predicate = Expression.Lambda<Func<T, bool>>(notDeleted, parameter);

            return query.Where(predicate);
        }
    }
}
