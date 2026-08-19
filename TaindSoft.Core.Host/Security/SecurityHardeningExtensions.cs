using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TaindSoft.Core.Host.Security
{
    /// <summary>
    /// Security hardening extensions for HTTP headers and protections
    /// </summary>
    public static class SecurityHardeningExtensions
    {
        /// <summary>
        /// Add security headers middleware
        /// Includes: HSTS, X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, Referrer-Policy
        /// </summary>
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                // HTTP Strict Transport Security (HSTS) - only in HTTPS
                if (context.Request.IsHttps)
                {
                    context.Response.Headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains";
                }

                // Prevent MIME type sniffing
                context.Response.Headers.XContentTypeOptions = "nosniff";

                // Prevent clickjacking
                context.Response.Headers.XFrameOptions = "DENY";

                // Enable browser XSS protection
                context.Response.Headers.XXSSProtection = "1; mode=block";

                // Referrer policy
                context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

                // Content Security Policy
                // NOTE: This inline CSP is obsolete. Use CspMiddleware and AddTaindSoftCsp() instead.
                // See: TaindSoft.Core.Host.Security.CspMiddleware, CspExtensions
                // context.Response.Headers.ContentSecurityPolicy = "default-src 'self'";

                // Remove server header
                _ = context.Response.Headers.Remove("Server");
                _ = context.Response.Headers.Remove("X-Powered-By");

                await next();
            });
        }

        /// <summary>
        /// Add CORS with strict configuration for frontend development
        /// </summary>
        public static IServiceCollection AddSecureCors(
            this IServiceCollection services,
            string[]? allowedOrigins = null,
            string[]? allowedMethods = null,
            string[]? allowedHeaders = null)
        {
            allowedOrigins ??= ["http://localhost:3000", "http://localhost:3173"];
            allowedMethods ??= ["GET", "POST", "PUT", "DELETE", "OPTIONS"];
            allowedHeaders ??= ["Content-Type", "Authorization", "X-Correlation-ID"];

            _ = services.AddCors(options =>
            {
                options.AddPolicy("SecureApi", policy =>
                {
                    _ = policy
                        .WithOrigins(allowedOrigins)
                        .WithMethods(allowedMethods)
                        .WithHeaders(allowedHeaders)
                        .AllowCredentials()
                        .WithExposedHeaders("X-Correlation-ID");
                });
            });

            return services;
        }

        /// <summary>
        /// Configure HTTPS and Kestrel security settings
        /// </summary>
        public static WebApplicationBuilder ConfigureHttpsSecurity(this WebApplicationBuilder builder)
        {
            _ = builder.Services.AddHttpsRedirection(options =>
            {
                options.HttpsPort = 443;
                options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
            });

            _ = builder.Services.AddHsts(options =>
            {
                options.MaxAge = TimeSpan.FromDays(365);
                options.IncludeSubDomains = true;
                options.Preload = true;
            });

            return builder;
        }

        /// <summary>
        /// Enable rate limiting for sensitive endpoints
        /// Requires: dotnet add package AspNetCoreRateLimit
        /// </summary>
        public static IServiceCollection AddRateLimiting(
            this IServiceCollection services,
            int requestsPerMinute = 100)
        {
            // Rate limiting configuration
            // This is a placeholder - actual implementation requires AspNetCoreRateLimit
            _ = services.AddScoped(sp => new RateLimitConfig { RequestsPerMinute = requestsPerMinute });
            return services;
        }
    }

    /// <summary>
    /// Rate limit configuration
    /// </summary>
    public class RateLimitConfig
    {
        public int RequestsPerMinute { get; set; } = 100;
        public int RequestsPerHour { get; set; } = 1000;
    }

    /// <summary>
    /// Security audit middleware to log suspicious activities
    /// </summary>
    public class SecurityAuditMiddleware(RequestDelegate next, ILogger<SecurityAuditMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<SecurityAuditMiddleware> _logger = logger;
        private readonly string[] _suspiciousPaths = ["/admin", "/../", "/..\\", "web.config"];

        public async Task InvokeAsync(HttpContext context)
        {
            string path = context.Request.Path.Value ?? "";

            // Log suspicious requests
            if (_suspiciousPaths.Any(p => path.Contains(p, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Suspicious request detected: {Path} from {IpAddress}", path, context.Connection.RemoteIpAddress);
            }

            // Log failed authentication attempts
            if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
            {
                _logger.LogWarning("Unauthorized access attempt: {Method} {Path} from {IpAddress}",
                    context.Request.Method, path, context.Connection.RemoteIpAddress);
            }

            await _next(context);
        }
    }

    /// <summary>
    /// Extension to register security audit middleware
    /// </summary>
    public static class SecurityAuditMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityAudit(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SecurityAuditMiddleware>();
        }
    }
}
