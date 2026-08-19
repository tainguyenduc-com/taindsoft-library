using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using TaindSoft.Core.Dtos;

namespace TaindSoft.Core.Host.Middleware
{
    /// <summary>
    /// Global exception handling middleware
    /// Converts exceptions to RFC 7807 ProblemDetails responses with structured logging
    /// </summary>
    public class GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger, IHostEnvironment env)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger = logger;
        private readonly IHostEnvironment _env = env;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Get correlation ID from context (set by CorrelationIdMiddleware)
                // If missing, generate new one (should never happen if middleware order is correct)
                string correlationId = context.Items["CorrelationId"]?.ToString()
                    ?? context.TraceIdentifier
                    ?? Guid.NewGuid().ToString("N");

                // Log with appropriate level based on exception type
                LogException(ex, context, correlationId);

                // Handle exception and return response
                await HandleExceptionAsync(context, ex, correlationId);
            }
        }

        private void LogException(Exception exception, HttpContext context, string correlationId)
        {
            LogLevel logLevel = GetLogLevel(exception);
            (int statusCode, string _, string _, Dictionary<string, string[]>? _) = MapExceptionToResponse(exception);
            string userId = context.User?.FindFirst("sub")?.Value
                      ?? context.User?.FindFirst("oid")?.Value
                      ?? "anonymous";

            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("Path", context.Request.Path))
            using (LogContext.PushProperty("Method", context.Request.Method))
            using (LogContext.PushProperty("UserId", userId))
            using (LogContext.PushProperty("ResponseStatusCode", statusCode))
            {
                if (logLevel == LogLevel.Error)
                {
                    _logger.LogError(exception,
                        "Unhandled exception [{ExceptionType}] → HTTP {StatusCode}: {ExceptionMessage}",
                        exception.GetType().Name, statusCode, exception.Message);
                }
                else
                {
                    _logger.LogWarning(exception,
                        "Client error [{ExceptionType}] → HTTP {StatusCode}: {ExceptionMessage}",
                        exception.GetType().Name, statusCode, exception.Message);
                }
            }
        }

        private static LogLevel GetLogLevel(Exception exception)
        {
            return exception switch
            {
                // 4xx errors - client mistakes (log as Warning)
                BadHttpRequestException => LogLevel.Warning,
                JsonException => LogLevel.Warning,
                ValidationException => LogLevel.Warning,
                NotFoundException => LogLevel.Warning,
                EntityNotFoundException => LogLevel.Warning,
                UnauthorizedAccessException => LogLevel.Warning,
                UnauthorizedException => LogLevel.Warning,
                InvalidOperationException => LogLevel.Warning,
                InvalidOperationExceptionEx => LogLevel.Warning,
                InvalidArgumentException => LogLevel.Warning,
                ArgumentNullException => LogLevel.Warning,
                ArgumentException => LogLevel.Warning,
                BusinessRuleViolatedException => LogLevel.Warning,

                // 5xx errors - server problems (log as Error)
                _ => LogLevel.Error
            };
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
        {
            (int statusCode, string message, string errorCode, Dictionary<string, string[]>? errors) = MapExceptionToResponse(exception);

            // Prepare response
            context.Response.StatusCode = statusCode;
            const string contentType = "application/json; charset=utf-8";
            context.Response.ContentType = contentType;

            // Determine canonical correlation id from request context
            string traceId = context.TraceIdentifier ?? correlationId ?? Activity.Current?.Id ?? Guid.NewGuid().ToString();

            // Create ApiResponse instance via reflection (use short type names via using)
            Type apiType = typeof(ApiResponse);
            object responseObj = Activator.CreateInstance(apiType)!;

            void TrySet(string name, object? value)
            {
                PropertyInfo? prop = apiType.GetProperty(name);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(responseObj, value);
                }
            }

            TrySet("Success", false);
            // prefer ErrorCode, fall back to Code
            TrySet("ErrorCode", errorCode ?? ErrorCodes.Unknown);
            TrySet("Message", message);
            TrySet("CorrelationId", traceId);
            TrySet("Errors", errors);

            // Ensure correlation id header (single source of truth)
            context.Response.Headers.TryAdd("X-Correlation-Id", traceId);

            // Reuse shared JSON options if present
            JsonSerializerOptions? jsonOptions = null;
            if (context.RequestServices.GetService(typeof(IOptions<JsonOptions>)) is IOptions<JsonOptions> httpJsonOptions)
            {
                jsonOptions = httpJsonOptions.Value?.SerializerOptions;
            }

            jsonOptions ??= new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            await context.Response.WriteAsJsonAsync(responseObj, jsonOptions, contentType, context.RequestAborted);
        }

        private static (int statusCode, string message, string errorCode, Dictionary<string, string[]>? errors) MapExceptionToResponse(Exception exception)
        {
            return exception switch
            {
                // 4xx Client Errors - Validation & Bad Requests
                BadHttpRequestException =>
                    (StatusCodes.Status400BadRequest, "Malformed JSON request body.", ErrorCodes.InvalidArgument, null),

                JsonException =>
                    (StatusCodes.Status400BadRequest, "Malformed JSON request body.", ErrorCodes.InvalidArgument, null),
                ValidationException validEx =>
                    (StatusCodes.Status400BadRequest, validEx.Message, ErrorCodes.ValidationFailed, validEx.Errors),

                InvalidOperationException invalidOpBcl =>
                    (StatusCodes.Status400BadRequest, invalidOpBcl.Message, ErrorCodes.InvalidOperation, null),
                InvalidOperationExceptionEx invalidOpEx =>
                    (StatusCodes.Status400BadRequest, invalidOpEx.Message, ErrorCodes.InvalidOperation, null),
                InvalidArgumentException invalidArgEx =>
                    (StatusCodes.Status400BadRequest, invalidArgEx.Message, ErrorCodes.InvalidArgument, null),

                ArgumentNullException =>
                    (StatusCodes.Status400BadRequest, "Required argument is null", ErrorCodes.InvalidArgument, null),

                ArgumentException argEx =>
                    (StatusCodes.Status400BadRequest, argEx.Message, ErrorCodes.InvalidArgument, null),

                // 401 Unauthorized
                UnauthorizedAccessException =>
                    (StatusCodes.Status401Unauthorized, "Access denied", ErrorCodes.Unauthorized, null),
                UnauthorizedException unauthorizedEx =>
                    (StatusCodes.Status401Unauthorized, unauthorizedEx.Message, ErrorCodes.Unauthorized, null),
                NotFoundException notFoundEx =>
                    (StatusCodes.Status404NotFound, notFoundEx.Message, ErrorCodes.NotFound, null),
                EntityNotFoundException entityNotFoundEx =>
                    (StatusCodes.Status404NotFound, entityNotFoundEx.Message, ErrorCodes.NotFound, null),
                BusinessRuleViolatedException businessRuleEx =>
                    (StatusCodes.Status409Conflict, businessRuleEx.Message, ErrorCodes.BusinessRuleViolation, null),

                // 5xx Server Errors
                DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "The resource was modified by another process. Please retry.", ErrorCodes.ConcurrencyConflict, null),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please contact support with the correlation ID.", ErrorCodes.InternalServerError, null)
            };
        }
    }

    /// <summary>
    /// Extension to register global exception handling
    /// </summary>
    public static class GlobalExceptionHandlingExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        }
    }
}
