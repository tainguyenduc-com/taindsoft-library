using Microsoft.Extensions.Logging;
using System.Diagnostics;
using TaindSoft.Core.Application.CQRS.CQRSPipelines;

namespace TaindSoft.Core.Application.CQRS.Pipelines
{
    /// <summary>
    /// CQRS logging pipeline behavior.
    /// Tracks execution time and logs command/query performance.
    /// </summary>
    public class LoggingPipelineBehavior<TRequest, TResponse>(ILogger<LoggingPipelineBehavior<TRequest, TResponse>> logger) : ICQRSPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ILogger<LoggingPipelineBehavior<TRequest, TResponse>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task<TResponse> Handle(
            TRequest request,
            Func<Task<TResponse>> next,
            CancellationToken cancellationToken)
        {
            string requestName = typeof(TRequest).Name;
            Stopwatch stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("Executing {RequestType}", requestName);

            try
            {
                TResponse response = await next();
                stopwatch.Stop();

                _logger.LogInformation(
                    "Completed {RequestType} in {ElapsedMilliseconds}ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);

                // Warn on slow requests (>1000ms)
                if (stopwatch.ElapsedMilliseconds > 1000)
                {
                    _logger.LogWarning(
                        "Slow request detected: {RequestType} took {ElapsedMilliseconds}ms",
                        requestName,
                        stopwatch.ElapsedMilliseconds);
                }

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "Failed {RequestType} after {ElapsedMilliseconds}ms. Error: {ErrorMessage}",
                    requestName,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }
}
