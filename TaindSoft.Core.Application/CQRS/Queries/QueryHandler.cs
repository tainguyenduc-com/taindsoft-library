namespace TaindSoft.Core.Application.CQRS.Queries
{
    /// <summary>
    /// Base handler for queries
    /// </summary>
    /// <typeparam name="TQuery">Query type</typeparam>
    /// <typeparam name="TResult">Result type</typeparam>
    public abstract class QueryHandler<TQuery, TResult> : IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        /// <summary>
        /// Handle the query
        /// </summary>
        public abstract Task<TResult> Handle(TQuery query, CancellationToken cancellationToken = default);
    }
}
