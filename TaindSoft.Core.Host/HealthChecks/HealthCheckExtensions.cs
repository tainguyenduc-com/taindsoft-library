using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace TaindSoft.Core.Host.HealthChecks
{
    /// <summary>
    /// Production-safe health check configuration.
    /// Provides /health/live and /health/ready endpoints.
    /// </summary>
    public static class HealthCheckExtensions
    {
        /// <summary>
        /// Adds health checks for outbox backlog.
        /// Database and Redis checks can be added via AddDbContextCheck and AddRedis when packages are available.
        /// </summary>
        public static IServiceCollection AddProductionHealthChecks(
            this IServiceCollection services)
        {
            IHealthChecksBuilder healthChecksBuilder = services.AddHealthChecks();

            // Custom outbox backlog check
            healthChecksBuilder.AddCheck<OutboxBacklogHealthCheck>(
                "outbox-backlog",
                failureStatus: HealthStatus.Degraded,
                tags: ["ready", "outbox"]);

            return services;
        }

        /// <summary>
        /// Maps health check endpoints: /health/live and /health/ready.
        /// </summary>
        public static IApplicationBuilder UseProductionHealthChecks(this IApplicationBuilder app)
        {
            app.UseHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false, // No checks, always returns healthy
                ResponseWriter = async (context, report) => await WriteHealthCheckResponse(context, report, new JsonSerializerOptions())
            });

            app.UseHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
                ResponseWriter = async (context, report) => await WriteHealthCheckResponse(context, report, new JsonSerializerOptions())
            });

            return app;
        }

        private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report, JsonSerializerOptions jsonSerializerOptions)
        {
            context.Response.ContentType = "application/json";

            var response = new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    duration = entry.Value.Duration.TotalMilliseconds,
                    tags = entry.Value.Tags
                }),
                totalDuration = report.TotalDuration.TotalMilliseconds
            };


            await context.Response.WriteAsync(
                JsonSerializer.Serialize(jsonSerializerOptions));
        }
    }
}
