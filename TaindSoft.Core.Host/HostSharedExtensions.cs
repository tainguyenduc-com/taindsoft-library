using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using System.Reflection;
using TaindSoft.Core.Host.Middleware;
using TaindSoft.Core.Host.Security;
using TaindSoft.Core.HttpApi.Endpoints;

namespace TaindSoft.Core.Host
{
    /// <summary>
    /// TODO: Document class HostSharedExtensions
    /// </summary>
    public static class HostSharedExtensions
    {
        public static IServiceCollection AddCustomJsonSerialization(this IServiceCollection services)
        {
            services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            });
            return services;
        }

        public static IServiceCollection AddCustomModelValidation(this IServiceCollection services)
        {
            services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    string correlationId = context.HttpContext.Items["CorrelationId"]?.ToString()
                        ?? context.HttpContext.TraceIdentifier
                        ?? Guid.NewGuid().ToString("N");

                    var errors = context.ModelState
                        .Where(kvp => kvp.Value?.Errors.Count > 0)
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

                    var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
                    {
                        Type = "https://httpstatuses.com/400",
                        Title = "Validation Error",
                        Status = 400,
                        Detail = "One or more validation errors occurred.",
                        Instance = context.HttpContext.Request.Path
                    };

                    problemDetails.Extensions["correlationId"] = correlationId;
                    problemDetails.Extensions["errors"] = errors;

                    var result = new Microsoft.AspNetCore.Mvc.ObjectResult(problemDetails)
                    {
                        StatusCode = 400,
                        ContentTypes = { "application/problem+json" }
                    };

                    return result;
                };
            });
            return services;
        }

        public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration, string policyName = "Frontend")
        {
            string[] configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            string[] allowedOrigins = configuredOrigins.Where(origin => !string.IsNullOrWhiteSpace(origin)).ToArray();

            if (allowedOrigins.Length == 0)
            {
                string? csvOrigins = configuration["Cors:AllowedOrigins"];
                if (!string.IsNullOrWhiteSpace(csvOrigins))
                {
                    allowedOrigins = csvOrigins
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Where(origin => !string.IsNullOrWhiteSpace(origin)).ToArray();
                }
            }

            services.AddCors(options =>
            {
                options.AddPolicy(policyName, policy =>
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials());

                options.AddPolicy("AdminSpa", policy =>
                    policy.WithOrigins("https://admin.taindsoft.local")
                          .AllowAnyHeader()
                          .WithMethods("GET", "POST")
                          .AllowCredentials());
            });
            return services;
        }

        public static WebApplication UseCustomOpenApi(this WebApplication app)
        {
            try
            {
                bool useOpenApi = app.Configuration.GetValue("UseOpenApi", false);
                if (useOpenApi)
                {
                    app.MapOpenApi().AllowAnonymous();
                }
            }
            catch { }
            return app;
        }

        public static WebApplication UseCustomMiddlewares(this WebApplication app)
        {
            app.UseCorrelationIdEnrichment();
            app.UseSerilogRequestLogging(options =>
            {
                options.MessageTemplate =
                    "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms" +
                    " | User:{UserId} Correlation:{CorrelationId} IP:{ClientIp}";

                options.GetLevel = (ctx, _, ex) =>
                    ex != null || ctx.Response.StatusCode >= 500
                        ? LogEventLevel.Error
                        : ctx.Response.StatusCode >= 400
                            ? LogEventLevel.Warning
                            : LogEventLevel.Information;

                options.EnrichDiagnosticContext = (diag, ctx) =>
                {
                    diag.Set("UserId", "anonymous");
                    diag.Set("CorrelationId", ctx.Items["CorrelationId"]?.ToString() ?? ctx.TraceIdentifier);
                    diag.Set("ClientIp", ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown");
                    diag.Set("UserAgent", ctx.Request.Headers.UserAgent.ToString());
                    if (ctx.Request.QueryString.HasValue)
                    {
                        diag.Set("QueryString", ctx.Request.QueryString.Value);
                    }
                };
            });

            app.UseGlobalExceptionHandling();
            return app;
        }

        public static WebApplication UseExportHubIfAvailable(this WebApplication app)
        {
            try
            {
                Type? exportHubType = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a =>
                {
                    try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                }).FirstOrDefault(t => t.FullName == "TaindSoft.SystemManagement.Infrastructure.Hubs.ExportHub");

                if (exportHubType != null)
                {
                    try
                    {
                        var mapHubMethods = typeof(HubEndpointRouteBuilderExtensions)
                            .GetMethods(BindingFlags.Public | BindingFlags.Static)
                            .Where(m => m.Name == "MapHub" && m.IsGenericMethodDefinition)
                            .ToList();

                        var mapHubMethod = mapHubMethods.FirstOrDefault(m => m.GetParameters().Length >= 2);
                        if (mapHubMethod != null)
                        {
                            var generic = mapHubMethod.MakeGenericMethod(exportHubType);
                            generic.Invoke(null, new object?[] { app, "/hubs/export", null });
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return app;
        }

        public static WebApplication UseModuleEndpoints(this WebApplication app)
        {
            try
            {
                var endpointTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => { try { return s.GetTypes(); } catch { return Array.Empty<Type>(); } })
                    .Where(t => typeof(IEndpoint).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                Log.Information($"Discovering endpoints... Found {endpointTypes.Count} endpoint types");

                foreach (var endpointType in endpointTypes)
                {
                    try
                    {
                        if (ActivatorUtilities.CreateInstance(app.Services, endpointType) is IEndpoint instance)
                        {
                            instance.MapEndpoint(app);
                            Log.Debug($"Mapped endpoint: {endpointType.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Failed to map endpoint: {endpointType.Name}");
                    }
                }

                Log.Information($"Successfully mapped {endpointTypes.Count} endpoints");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during endpoint discovery and mapping");
            }
            return app;
        }

        public static WebApplication UseHostDefaults(this WebApplication app)
        {
            // Group common host pipeline setup to reduce repeated call sites.
            app.UseCustomMiddlewares();
            app.UseCustomOpenApi();
            app.UseExportHubIfAvailable();
            return app;
        }

        public static IServiceCollection AddHostDefaults(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddCustomJsonSerialization();
            services.AddCustomModelValidation();
            services.AddCustomCors(configuration);
            services.AddOpenApi(); // ponytail: native OpenApi doc gen for /openapi/v1.json
            return services;
        }

        /// <summary>
        /// Register token endpoint rate limiting (for /connect/token).
        /// Bound from "TokenRateLimit" config section.
        /// </summary>
        public static IServiceCollection AddTokenEndpointRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<TokenRateLimitOptions>(configuration.GetSection("TokenRateLimit"));
            return services;
        }

        /// <summary>
        /// Enable token endpoint rate limiting middleware.
        /// Must be called before UseRouting.
        /// </summary>
        public static IApplicationBuilder UseTokenEndpointRateLimiter(this IApplicationBuilder app)
        {
            return app.UseMiddleware<TokenEndpointRateLimiter>();
        }

        /// <summary>
        /// Register the authentication audit logger implementation.
        /// Use this from the Host to register the host-level implementation.
        /// </summary>
        public static IServiceCollection AddAuthenticationAuditLogger<T>(this IServiceCollection services)
            where T : class, IAuthenticationAuditLogger
        {
            services.AddScoped<IAuthenticationAuditLogger, T>();
            return services;
        }

        /// <summary>
        /// Enable authentication audit middleware (tracks /connect/token, /connect/revoke events).
        /// Must be placed after UseAuthentication/UseAuthorization in the pipeline.
        /// </summary>
        public static IApplicationBuilder UseAuthenticationAudit(this IApplicationBuilder app)
        {
            return app.UseMiddleware<AuthenticationAuditMiddleware>();
        }
    }
}
