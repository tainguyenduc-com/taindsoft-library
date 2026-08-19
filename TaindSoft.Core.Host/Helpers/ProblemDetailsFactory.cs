using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace TaindSoft.Core.Host.Helpers
{
    /// <summary>
    /// Centralized factory for creating RFC 7807 ProblemDetails responses
    /// Ensures consistent error format across all endpoints and middleware
    /// </summary>
    public static class ProblemDetailsFactory
    {
        /// <summary>
        /// Create RFC 7807 compliant ProblemDetails with correlationId
        /// </summary>
        public static ProblemDetails CreateProblemDetails(
            int statusCode,
            string title,
            string detail,
            string instance,
            string correlationId,
            Dictionary<string, string[]>? errors = null)
        {
            ProblemDetails problemDetails = new()
            {
                Type = $"https://httpstatuses.com/{statusCode}",
                Title = title,
                Status = statusCode,
                Detail = detail,
                Instance = instance,
                Extensions = new Dictionary<string, object?>
                {
                    { "correlationId", correlationId }
                }
            };

            // Add validation errors if present
            if (errors != null && errors.Count > 0)
            {
                problemDetails.Extensions["errors"] = errors;
            }

            return problemDetails;
        }

        /// <summary>
        /// Serialize ProblemDetails to JSON with correct Content-Type
        /// </summary>
        public static async Task WriteProblemDetailsAsync(
            HttpContext context,
            ProblemDetails problemDetails,
            JsonSerializerOptions jsonSerializerOptions,
            CancellationToken cancellationToken = default)
        {
            context.Response.StatusCode = problemDetails.Status ?? 500;
            context.Response.ContentType = "application/problem+json; charset=utf-8";

            string json = JsonSerializer.Serialize(problemDetails, jsonSerializerOptions);

            await context.Response.WriteAsync(json, cancellationToken);
        }

        /// <summary>
        /// Get CorrelationId from HttpContext (with safe fallback)
        /// </summary>
        public static string GetCorrelationId(HttpContext context)
        {
            return context.Items["CorrelationId"]?.ToString()
                ?? context.TraceIdentifier
                ?? Guid.NewGuid().ToString("N");
        }
    }
}
