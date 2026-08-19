using System.Linq.Expressions;
using System.Reflection;
using TaindSoft.Core.Application.Abstractions;

namespace TaindSoft.Core.Infrastructure.Extensions
{
    /// <summary>
    /// TODO: Document class SortingExtensions
    /// </summary>
    public static class SortingExtensions
    {
        public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, ISortableRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.SortBy))
            {
                return query;
            }

            PropertyInfo? property = typeof(T).GetProperty(request.SortBy);
            if (property == null)
            {
                return query;
            }

            ParameterExpression parameter = Expression.Parameter(typeof(T), "entity");
            MemberExpression propertyAccess = Expression.Property(parameter, property);
            LambdaExpression keySelector = Expression.Lambda(propertyAccess, parameter);

            string methodName = request.Desc ? "OrderByDescending" : "OrderBy";
            MethodInfo method = typeof(Queryable).GetMethods()
                .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), property.PropertyType);

            return (IQueryable<T>)method.Invoke(null, [query, keySelector])!;
        }
    }
}
