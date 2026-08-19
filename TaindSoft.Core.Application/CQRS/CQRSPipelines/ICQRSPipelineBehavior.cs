namespace TaindSoft.Core.Application.CQRS.CQRSPipelines
{
    /// <summary>
    /// Pipeline behavior interface for CQRS
    /// </summary>
    public interface ICQRSPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken);
    }
}
