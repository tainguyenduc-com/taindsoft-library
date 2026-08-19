using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using TaindSoft.Core.Identity.Configuration;

namespace TaindSoft.Core.Host.Security.Jwt
{
    /// <summary>
    /// TODO: Document class MonolithJwtExtensions
    /// </summary>
    public static class MonolithJwtExtensions
    {
        public static IServiceCollection AddMonolithJwt(
            this IServiceCollection services)
        {
            IConfiguration configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            JsonSerializerOptions jsonOptions = services.BuildServiceProvider().GetRequiredService<IOptions<JsonSerializerOptions>>().Value ?? new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            IConfigurationSection jwtSection = configuration.GetSection("JWT");
            services.Configure<JwtOptions>(jwtSection);

            string? jwtSecret = configuration["JWT:HmacSecret"];
            string? jwtIssuer = configuration["JWT:Issuer"];
            string? jwtAudience = configuration["JWT:Audiences"] ?? configuration["JWT:Audience"];

            jwtIssuer = jwtIssuer?.Trim();
            if (!string.IsNullOrEmpty(jwtIssuer))
            {
                jwtIssuer = jwtIssuer!.TrimEnd('/');
            }

            jwtAudience = jwtAudience?.Trim();

            if (string.IsNullOrWhiteSpace(jwtSecret))
            {
                throw new InvalidOperationException(
                    "JWT:HmacSecret is required for monolith strategy. " +
                    "Generate using: openssl rand -hex 32");
            }

            if (string.IsNullOrWhiteSpace(jwtIssuer))
            {
                throw new InvalidOperationException("JWT:Issuer is required.");
            }

            if (string.IsNullOrWhiteSpace(jwtAudience))
            {
                throw new InvalidOperationException("JWT:Audiences (or JWT:Audience) is required.");
            }

            byte[] secretBytes = Encoding.UTF8.GetBytes(jwtSecret);

            if (secretBytes.Length < 32)
            {
                throw new InvalidOperationException(
                    $"JWT:HmacSecret must be at least 256 bits (32 bytes). " +
                    $"Current: {secretBytes.Length} bytes. " +
                    "Generate secure key: openssl rand -hex 32");
            }

            string[] skipTokenValidationPaths = ResolveSkipTokenValidationPaths(configuration);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(secretBytes),
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (ShouldSkipTokenValidation(context.HttpContext.Request.Path, skipTokenValidationPaths))
                        {
                            // Skip token extraction/validation for configured public paths.
                            context.NoResult();
                        }

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception != null)
                        {
                            ILogger logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("JWT.Authentication");
                            logger.LogError(context.Exception, "JWT authentication failed: {Message}",
                                context.Exception.Message);
                        }

                        return Task.CompletedTask;
                    },

                    OnTokenValidated = context =>
                    {
                        string? userId = context.Principal?.FindFirst(
                            ClaimTypes.NameIdentifier)?.Value;

                        ILogger logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JWT.Authentication");
                        logger.LogInformation("JWT validated successfully for user {UserId}", userId);

                        return Task.CompletedTask;
                    },

                    OnChallenge = async context =>
                    {
                        context.HandleResponse();

                        string correlationId = context.HttpContext.Items["CorrelationId"]?.ToString()
                            ?? context.HttpContext.TraceIdentifier
                            ?? Guid.NewGuid().ToString("N");

                        ProblemDetails problemDetails = new()
                        {
                            Type = "https://httpstatuses.com/401",
                            Title = "Unauthorized",
                            Status = 401,
                            Detail = "Authentication is required to access this resource.",
                            Instance = context.HttpContext.Request.Path,
                            Extensions = new Dictionary<string, object?>
                            {
                                { "correlationId", correlationId }
                            }
                        };

                        context.HttpContext.Response.StatusCode = 401;
                        context.HttpContext.Response.ContentType = "application/problem+json; charset=utf-8";

                        string json = JsonSerializer.Serialize(problemDetails,
                            jsonOptions);

                        await context.HttpContext.Response.WriteAsync(json);
                    },

                    OnForbidden = async context =>
                    {
                        string correlationId = context.HttpContext.Items["CorrelationId"]?.ToString()
                            ?? context.HttpContext.TraceIdentifier
                            ?? Guid.NewGuid().ToString("N");

                        ProblemDetails problemDetails = new()
                        {
                            Type = "https://httpstatuses.com/403",
                            Title = "Forbidden",
                            Status = 403,
                            Detail = "You do not have permission to access this resource.",
                            Instance = context.HttpContext.Request.Path,
                            Extensions = new Dictionary<string, object?>
                            {
                                { "correlationId", correlationId }
                            }
                        };

                        context.HttpContext.Response.StatusCode = 403;
                        context.HttpContext.Response.ContentType = "application/problem+json; charset=utf-8";

                        string json = JsonSerializer.Serialize(problemDetails,
                            jsonOptions);

                        await context.HttpContext.Response.WriteAsync(json);
                    }
                };
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("ApiJwt", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                });
            });

            return services;
        }

        private static string[] ResolveSkipTokenValidationPaths(IConfiguration configuration)
        {
            // Primary key for this behavior
            string[]? configured = configuration.GetSection("JWT:SkipTokenValidationPaths").Get<string[]>();

            // Backward-compatible key from previous iterations
            if (configured == null || configured.Length == 0)
            {
                configured = configuration.GetSection("Jwt:PublicApiPrefixes").Get<string[]>();
            }

            if (configured == null || configured.Length == 0)
            {
                return [];
            }

            return configured
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(NormalizePathPrefix)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static bool ShouldSkipTokenValidation(PathString requestPath, IEnumerable<string> skipPrefixes)
        {
            foreach (string prefix in skipPrefixes)
            {
                if (requestPath.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizePathPrefix(string path)
        {
            string normalized = path.Trim();
            if (!normalized.StartsWith('/'))
            {
                normalized = "/" + normalized;
            }

            return normalized.TrimEnd('/');
        }
    }
}
