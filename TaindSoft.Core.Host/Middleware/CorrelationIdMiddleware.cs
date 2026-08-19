using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace TaindSoft.Core.Host.Middleware
{
    /// <summary>
    /// Middleware to enrich logs with correlation ID and request tracking
    /// Adds CorrelationId and RequestId to LogContext for structured logging
    /// </summary>
    public class CorrelationIdMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;
        private const string CorrelationIdHeaderName = "X-Correlation-ID";
        private const string RequestIdHeaderName = "X-Request-ID";

        public async Task InvokeAsync(HttpContext context)
        {
            // Extract or generate CorrelationId
            string correlationId = ExtractCorrelationId(context);

            // Store in HttpContext.Items for other middleware to access
            context.Items["CorrelationId"] = correlationId;

            // Extract or generate RequestId
            string requestId = context.TraceIdentifier;

            // Push correlation context for Serilog enrichment
            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("RequestId", requestId))
            using (LogContext.PushProperty("UserId", "anonymous"))
            using (LogContext.PushProperty("RemoteIp", context.Connection.RemoteIpAddress?.ToString() ?? "unknown"))
            using (LogContext.PushProperty("Method", context.Request.Method))
            using (LogContext.PushProperty("Path", context.Request.Path))
            {
                // Add correlation ID to response headers for client tracking
                context.Response.OnStarting(() =>
                {
                    if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
                    {
                        context.Response.Headers[CorrelationIdHeaderName] = correlationId;
                    }
                    return Task.CompletedTask;
                });

                await _next(context);
            }
        }

        private static string ExtractCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out Microsoft.Extensions.Primitives.StringValues correlationIdValues))
            {
                string? firstValue = correlationIdValues.First();
                if (!string.IsNullOrEmpty(firstValue))
                {
                    return firstValue;
                }
            }

            if (context.Request.Headers.TryGetValue("traceparent", out Microsoft.Extensions.Primitives.StringValues traceparentValues))
            {
                // Extract from W3C Trace Context format: 00-trace-id-parent-id-flags
                string? firstTraceparent = traceparentValues.First();
                if (!string.IsNullOrEmpty(firstTraceparent))
                {
                    string[] traceparent = firstTraceparent.Split('-');
                    if (traceparent.Length >= 2)
                    {
                        return traceparent[1]; // trace-id
                    }
                }
            }

            // Use TraceIdentifier or generate new GUID
            return context.TraceIdentifier ?? Guid.NewGuid().ToString("N");
        }
    }

    /// <summary>
    /// Extension method to register correlation ID middleware
    /// </summary>
    public static class CorrelationIdMiddlewareExtensions
    {
        public static IApplicationBuilder UseCorrelationIdEnrichment(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CorrelationIdMiddleware>();
        }
    }
}
