namespace TaindSoft.Core.HttpApi.Endpoints
{
    /// <summary>
    /// Base interface for all HTTP endpoints following REPR (Request-Endpoint-Response) pattern
    /// </summary>
    public interface IEndpoint
    {
        void MapEndpoint(IEndpointRouteBuilder app);
    }

    /// <summary>
    /// Base interface for endpoints with request/response
    /// </summary>
    public interface IEndpoint<TRequest, TResponse> : IEndpoint
        where TRequest : class
    {
        Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Base interface for endpoints with only response (no request)
    /// </summary>
    public interface IEndpoint<TResponse> : IEndpoint
    {
        Task<TResponse> HandleAsync(CancellationToken cancellationToken);
    }
}
