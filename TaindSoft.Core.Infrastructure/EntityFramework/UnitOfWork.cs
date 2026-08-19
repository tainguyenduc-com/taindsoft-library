using Microsoft.EntityFrameworkCore.Storage;
using TaindSoft.Core.Domain.UnitOfworks;

namespace TaindSoft.Core.Infrastructure.EntityFramework
{
    /// <summary>
    /// Unit of Work implementation for managing multiple repositories and transactions
    /// </summary>
    /// <summary>
    /// Unit of Work wrapper around a DbContext to coordinate transactions and save operations.
    /// </summary>
    public class UnitOfWork<TDbContext>(TDbContext dbContext) : IUnitOfWork<TDbContext>
        where TDbContext : BaseDbContext
    {
        private readonly TDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

        public async Task ExecuteTransactionAsync(Func<Task> execute, CancellationToken cancellationToken = default)
        {
            // Don't use execution strategy wrapper - it can cause disposed context issues
            // The DbContext should be scoped and shared across repositories
            // If a transaction already exists on the DbContext, do not start a nested transaction.
            // Simply execute the delegate so callers that already started a transaction are respected.
            IDbContextTransaction? current = _dbContext.Database.CurrentTransaction;
            if (current != null)
            {
                // Already inside a transaction - just execute the action.
                await execute();
                return;
            }

            await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await execute();
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
