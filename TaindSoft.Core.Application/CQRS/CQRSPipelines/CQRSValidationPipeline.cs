namespace TaindSoft.Core.Application.CQRS.CQRSPipelines
{
    /// <summary>
    /// Pipeline for validation
    /// </summary>
    public sealed class CQRSValidationPipeline<TRequest, TResponse> : ICQRSPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken)
        {
            // Validation logic would go here
            return next();
        }
    }
}
