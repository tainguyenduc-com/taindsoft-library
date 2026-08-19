using System.Linq.Expressions;
using TaindSoft.Core.Domain.Entities;

namespace TaindSoft.Core.Domain.Specifications
{
    /// <summary>
    /// Specification pattern interface for composable and reusable query logic
    /// Encapsulates WHERE clauses, INCLUDE statements, ORDER BY, and PAGING
    /// </summary>
    /// <remarks>
    /// The Specification pattern allows you to encapsulate query logic in reusable objects.
    /// Instead of writing LINQ queries everywhere, define them once as specifications and reuse them.
    /// 
    /// Benefits:
    /// - DRY (Don't Repeat Yourself): Query logic defined once
    /// - Testable: Specifications are easy to unit test
    /// - Composable: Build complex queries from simple building blocks
    /// - Clear Intent: Specification names explain what data is being fetched
    /// 
    /// Example:
    /// <code>
    /// // Define once
    /// public class ActiveUsersSpec : Specification&lt;User&gt;
    /// {
    ///     public ActiveUsersSpec()
    ///     {
    ///         Criteria = u =&gt; u.IsActive &amp;&amp; !u.IsDeleted;
    ///         AddInclude(u =&gt; u.Roles);
    ///     }
    /// }
    /// 
    /// // Use everywhere
    /// var users = await _repository.ListAsync(new ActiveUsersSpec());
    /// </code>
    /// </remarks>
    public interface ISpecification<T> where T : Entity
    {
        /// <summary>
        /// The WHERE clause criteria
        /// </summary>
        Expression<Func<T, bool>>? Criteria { get; }

        /// <summary>
        /// Navigation properties to eagerly load
        /// </summary>
        IReadOnlyList<Expression<Func<T, object>>> Includes { get; }

        /// <summary>
        /// String-based include paths (for complex navigation)
        /// </summary>
        IReadOnlyList<string> IncludeStrings { get; }

        /// <summary>
        /// Primary ORDER BY clause
        /// </summary>
        Expression<Func<T, object>>? OrderBy { get; }

        /// <summary>
        /// Descending ORDER BY clause
        /// </summary>
        Expression<Func<T, object>>? OrderByDescending { get; }

        /// <summary>
        /// Whether pagination is applied
        /// </summary>
        bool IsPagingEnabled { get; }

        /// <summary>
        /// Number of records to skip
        /// </summary>
        int Skip { get; }

        /// <summary>
        /// Number of records to take
        /// </summary>
        int Take { get; }

        /// <summary>
        /// Whether to track entities (default: true for modifications, false for read-only)
        /// </summary>
        bool IsTrackingEnabled { get; }
    }
}
