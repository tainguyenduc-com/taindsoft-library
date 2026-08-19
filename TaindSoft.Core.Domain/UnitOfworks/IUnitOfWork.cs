namespace TaindSoft.Core.Domain.UnitOfworks
{
    /// <summary>
    /// Base unit of work interface for coordinating transactions (DbContext-agnostic)
    /// </summary>
    public interface IUnitOfWork
    {
        Task ExecuteTransactionAsync(Func<Task> execute, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Generic unit of work interface for coordinating multiple repositories with a specific DbContext
    /// </summary>
    public interface IUnitOfWork<TDbContext> : IUnitOfWork
    {
    }
}
