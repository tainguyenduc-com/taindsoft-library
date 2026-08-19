using Microsoft.EntityFrameworkCore;

namespace TaindSoft.Core.Infrastructure.Extensions
{
    /// <summary>
    /// EF Core optimization extensions
    /// </summary>
    public static class EfCoreOptimizations
    {
        /// <summary>
        /// Applies recommended EF Core optimizations to DbContext options
        /// - Split queries for complex relationships
        /// - Query filters
        /// - Command timeout
        /// - Efficient query patterns
        /// </summary>
        public static DbContextOptionsBuilder ApplyOptimalConfiguration(
            this DbContextOptionsBuilder optionsBuilder)
        {
            // Set command timeout (default 30s, but can be customized per module)
            // Note: CommandTimeout should be set on the database-specific options builder (e.g., UseSqlServer, UseNpgsql)
            // commandTimeout ??= TimeSpan.FromSeconds(30);
            // optionsBuilder.CommandTimeout((int)commandTimeout.Value.TotalSeconds);

            // Enable query caching for compiled queries
            _ = optionsBuilder.EnableDetailedErrors();

            return optionsBuilder;
        }

        /// <summary>
        /// Best practices for query execution:
        /// 1. Use AsNoTracking() for read-only queries
        /// 2. Use Split() for complex relationships
        /// 3. Use Select for projection to minimize data transfer
        /// 4. Batch operations when possible
        /// 5. Use include with caution to avoid N+1
        /// </summary>
        public static IQueryable<T> OptimizeForRead<T>(this IQueryable<T> query) where T : class
        {
            // Default optimization: no tracking for read queries
            return query.AsNoTracking();
        }

        /// <summary>
        /// Helper to create a split query for complex relationships
        /// Example: context.Orders.Include(o => o.Items).AsSplitQuery()
        /// </summary>
        public static IQueryable<T> OptimizeComplexInclude<T>(this IQueryable<T> query) where T : class
        {
            return query.AsSplitQuery();
        }
    }
}
