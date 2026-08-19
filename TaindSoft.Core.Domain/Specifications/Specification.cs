using System.Linq.Expressions;
using TaindSoft.Core.Domain.Entities;

namespace TaindSoft.Core.Domain.Specifications
{
    /// <summary>
    /// Base class for implementing specifications
    /// Provides fluent builder methods for common query operations
    /// </summary>
    /// <remarks>
    /// Inherit from this class to define reusable query specifications.
    /// 
    /// Example:
    /// <code>
    /// public class ProductsSpec : Specification&lt;Product&gt;
    /// {
    ///     public ProductsSpec(string category, bool includeArchived = false)
    ///     {
    ///         // Filter
    ///         Criteria = p =&gt; !p.IsDeleted &amp;&amp; 
    ///                         (includeArchived || !p.IsArchived) &amp;&amp;
    ///                         p.Category == category;
    ///         
    ///         // Includes
    ///         AddInclude(p =&gt; p.Category);
    ///         AddInclude(p =&gt; p.Reviews);
    ///         
    ///         // Order
    ///         AddOrderBy(p =&gt; p.Name);
    ///         
    ///         // Paging
    ///         ApplyPaging(0, 20);
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public abstract class Specification<T> : ISpecification<T> where T : Entity
    {
        private readonly List<Expression<Func<T, object>>> _includes = [];
        private readonly List<string> _includeStrings = [];

        public Expression<Func<T, bool>>? Criteria { get; protected set; }
        public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes.AsReadOnly();
        public IReadOnlyList<string> IncludeStrings => _includeStrings.AsReadOnly();
        public Expression<Func<T, object>>? OrderBy { get; protected set; }
        public Expression<Func<T, object>>? OrderByDescending { get; protected set; }
        public bool IsPagingEnabled { get; protected set; }
        public int Skip { get; protected set; }
        public int Take { get; protected set; }
        public bool IsTrackingEnabled { get; protected set; } = true;

        /// <summary>
        /// Adds an INCLUDE for eager loading of navigation properties
        /// </summary>
        /// <remarks>
        /// Use this to eagerly load related entities to avoid N+1 queries.
        /// 
        /// Example:
        /// <code>
        /// AddInclude(p =&gt; p.Category);        // Single navigation
        /// AddInclude(p =&gt; p.Reviews);        // Collection navigation
        /// </code>
        /// </remarks>
        protected void AddInclude(Expression<Func<T, object>> includeExpression)
        {
            ArgumentNullException.ThrowIfNull(includeExpression);

            _includes.Add(includeExpression);
        }

        /// <summary>
        /// Adds a string-based INCLUDE for complex navigation paths
        /// </summary>
        /// <remarks>
        /// Use this for deeply nested includes that can't be expressed as lambda expressions.
        /// 
        /// Example:
        /// <code>
        /// AddIncludeString("Category.Subcategory.Products");
        /// </code>
        /// </remarks>
        protected void AddIncludeString(string includeString)
        {
            if (string.IsNullOrWhiteSpace(includeString))
            {
                throw new ArgumentNullException(nameof(includeString));
            }

            _includeStrings.Add(includeString);
        }

        /// <summary>
        /// Sets the primary ORDER BY clause
        /// </summary>
        protected void AddOrderBy(Expression<Func<T, object>> orderByExpression)
        {
            ArgumentNullException.ThrowIfNull(orderByExpression);

            OrderBy = orderByExpression;
        }

        /// <summary>
        /// Sets the primary ORDER BY DESC clause
        /// </summary>
        protected void AddOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
        {
            ArgumentNullException.ThrowIfNull(orderByDescendingExpression);

            OrderByDescending = orderByDescendingExpression;
        }

        /// <summary>
        /// Applies pagination
        /// </summary>
        /// <remarks>
        /// Skip is typically calculated as (PageNumber - 1) * PageSize
        /// Take is the PageSize
        /// 
        /// Example:
        /// <code>
        /// ApplyPaging(0, 10);   // Get first 10 records
        /// ApplyPaging(10, 10);  // Get next 10 records (skip first 10)
        /// </code>
        /// </remarks>
        protected void ApplyPaging(int skip, int take)
        {
            if (skip < 0)
            {
                throw new ArgumentException("Skip must be >= 0", nameof(skip));
            }

            if (take <= 0)
            {
                throw new ArgumentException("Take must be > 0", nameof(take));
            }

            Skip = skip;
            Take = take;
            IsPagingEnabled = true;
        }

        /// <summary>
        /// Disables entity tracking for read-only queries (better performance)
        /// </summary>
        /// <remarks>
        /// Use this for queries where you won't modify the entities.
        /// Disabling tracking improves performance but entities won't be changed-tracked.
        /// 
        /// Example:
        /// <code>
        /// DisableTracking(); // For reporting/read-only queries
        /// </code>
        /// </remarks>
        protected void DisableTracking()
        {
            IsTrackingEnabled = false;
        }
    }
}
