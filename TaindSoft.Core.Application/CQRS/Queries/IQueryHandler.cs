namespace TaindSoft.Core.Application.CQRS.Queries
{
    /// <summary>
    /// Handler for queries
    /// </summary>
    public interface IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
    {
        Task<TResult> Handle(TQuery query, CancellationToken cancellationToken = default);
    }
}
