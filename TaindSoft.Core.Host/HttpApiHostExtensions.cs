using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using TaindSoft.Core.Config;
using TaindSoft.Core.Host.BackgroundTasks;
using TaindSoft.Core.Host.Middleware;
using TaindSoft.Core.Infrastructure.Auditing;
using TaindSoft.Core.PermissionCheckers;
using TaindSoft.Core.Settings;

namespace TaindSoft.Core.Host
{
    // Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
    // This project should be referenced by each service project in your solution.
    // To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
    /// <summary>
    /// TODO: Document class HttpApiHostExtensions
    /// </summary>
    public static class HttpApiHostExtensions
    {
        public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            // Configure OpenTelemetry (metrics + tracing + exporters)
            builder.ConfigureOpenTelemetry();

            // Register default health checks
            builder.AddDefaultHealthChecks();

            // Service discovery and resilient HTTP clients
            builder.Services.AddServiceDiscovery();

            // NOTE: Do NOT add http.AddServiceDiscovery() here globally.
            // Service discovery wraps ALL HTTP clients with ResolvingHttpDelegatingHandler
            // which tries to resolve hostnames as Aspire service endpoints.
            // In monolithic TaindSoft.Host, typed HTTP clients use explicit base addresses
            // (e.g., http://localhost:5000) for YARP gateway — service discovery
            // mangles these URIs producing invalid file:// scheme.
            // Individual clients can opt-in via .AddServiceDiscovery() if needed.
            builder.Services.ConfigureHttpClientDefaults(http =>
            {
                // Turn on resilience by default
                http.AddStandardResilienceHandler();
            });

            // Register HTTP-based config provider that queries system-management service.
            // Modules (e.g., SystemManagement) can replace this registration with a module-specific provider.
            builder.Services.AddCoreConfig(builder.Configuration);
            builder.Services.AddScoped<IPermissionChecker, NonePermissionChecker>();
            builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddScoped<ISettingsManager, SettingsManager>();
            // Ensure a default audit store exists to avoid core referencing module entities.
            // Modules (e.g., SystemManagement) can register their own ISystemAuditStore which will override this.
            builder.Services.TryAddScoped<ISystemAuditStore, NoOpSystemAuditStore>();

            return builder;
        }

        // Register background task queue and hosted worker
        public static TBuilder AddServiceDefaultsWithBackgroundQueue<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            _ = builder.AddServiceDefaults();
            _ = builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

            _ = builder.Services.AddHostedService<QueuedHostedService>();
            return builder;
        }

        // Register background task queue and hosted worker
        public static TBuilder AddBackgroundTaskQueue<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            _ = builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            _ = builder.Services.AddHostedService<QueuedHostedService>();
            return builder;
        }

        public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            builder.Services.AddHealthChecks()
                // Add a default liveness check to ensure app is responsive
                .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

            return builder;
        }

        public static WebApplication MapDefaultEndpoints(this WebApplication app)
        {
            // Adding health checks endpoints to applications in non-development environments has security implications.
            // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.

            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks("/health");

            if (app.Environment.IsDevelopment())
            {
                // Only health checks tagged with the "live" tag must pass for app to be considered alive
                app.MapHealthChecks("/alive", new HealthCheckOptions
                {
                    Predicate = r => r.Tags.Contains("live")
                });
            }

            // Backward-compatible simple endpoint
            _ = app.MapGet("/api/v1/health", () => Results.Ok(new { status = "Healthy" }));

            return app;
        }

        public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });

            builder.Services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics.AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation();
                })
                .WithTracing(tracing =>
                {
                    tracing.AddSource(builder.Environment.ApplicationName)
                        .AddAspNetCoreInstrumentation(tracingOptions =>
                        {
                            tracingOptions.Filter = context =>
                                !context.Request.Path.StartsWithSegments("/health")
                                && !context.Request.Path.StartsWithSegments("/alive");
                        })
                        .AddHttpClientInstrumentation();
                });

            builder.AddOpenTelemetryExporters();

            return builder;
        }

        private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            bool useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

            if (useOtlpExporter)
            {
                builder.Services.AddOpenTelemetry().UseOtlpExporter();
            }

            return builder;
        }

        /// <summary>
        /// Use validation exception handling middleware
        /// </summary>
        public static IApplicationBuilder UseValidationExceptionHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ValidationExceptionHandlingMiddleware>();
        }
    }
}
