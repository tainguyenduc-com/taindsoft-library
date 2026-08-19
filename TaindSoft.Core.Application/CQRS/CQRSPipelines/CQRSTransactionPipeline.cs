using TaindSoft.Core.Application.CQRS.Commands;
using TaindSoft.Core.Domain.UnitOfworks;

namespace TaindSoft.Core.Application.CQRS.CQRSPipelines
{
    /// <summary>
    /// Transaction pipeline for commands
    /// </summary>
    public sealed class CQRSTransactionPipeline<TRequest, TResponse>(IUnitOfWork? unitOfWork)
        : ICQRSPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IUnitOfWork? _unitOfWork = unitOfWork;

        public Task<TResponse> Handle(
            TRequest request,
            Func<Task<TResponse>> next,
            CancellationToken cancellationToken)
        {
            if (request is not ICommand)
            {
                return next();
            }

            if (_unitOfWork == null)
            {
                return next();
            }

            return ExecuteTransactionAsync(next, cancellationToken);
        }

        private async Task<TResponse> ExecuteTransactionAsync(Func<Task<TResponse>> next, CancellationToken cancellationToken)
        {
            TResponse? response = default;

            await _unitOfWork!.ExecuteTransactionAsync(async () =>
            {
                response = await next();
            }, cancellationToken);

            return response!;
        }
    }
}
