using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace TaindSoft.Core.Host.Middleware;

/// <summary>
/// Rate-limiting middleware for /connect/token endpoint.
/// Uses per-IP fixed-window counters to limit authorization_code and refresh_token requests.
/// Also blocks client_credentials grant (not used in this deployment).
/// Logs and blocks requests that exceed the configured threshold.
/// </summary>
public class TokenEndpointRateLimiter(
    RequestDelegate next,
    ILogger<TokenEndpointRateLimiter> logger,
    IOptions<TokenRateLimitOptions> options)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<TokenEndpointRateLimiter> _logger = logger;
    private readonly TokenRateLimitOptions _options = options.Value;

    // Per-IP fixed-window counters
    private readonly ConcurrentDictionary<string, RateWindow> _windows = new();

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method == HttpMethods.Post
            && context.Request.Path.StartsWithSegments("/connect/token", StringComparison.OrdinalIgnoreCase))
        {
            string clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string? clientId = null;

            if (context.Request.HasFormContentType)
            {
                var form = await context.Request.ReadFormAsync();
                string grantType = form["grant_type"].ToString();
                clientId = form["client_id"].ToString();

                // Block client_credentials grant — not used in this deployment.
                // Audit-log any attempt and return 403 Forbidden.
                if (string.Equals(grantType, "client_credentials", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "BLOCKED client_credentials grant attempt from {ClientIp} (client_id={ClientId}). " +
                        "This grant type is disabled in this deployment.",
                        clientIp, clientId ?? "unknown");

                    // Also log at critical level for security monitoring
                    _logger.LogCritical(
                        "SECURITY: client_credentials grant request blocked. client_id={ClientId}, ip={ClientIp}",
                        clientId ?? "unknown", clientIp);

                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "unauthorized_client",
                        error_description = "The client_credentials grant type is not allowed."
                    });
                    return;
                }

                // Rate-limit authorization_code and refresh_token
                if (grantType is "authorization_code" or "refresh_token")
                {
                    string rateKey = $"{clientIp}:{grantType}";
                    var now = DateTimeOffset.UtcNow;

                    var window = _windows.AddOrUpdate(
                        rateKey,
                        _ => new RateWindow(now, 1),
                        (_, existing) =>
                        {
                            existing.TryAdvance(now, _options.WindowSeconds);
                            existing.Count++;
                            return existing;
                        });

                    if (window.Count > _options.MaxRequestsPerWindow)
                    {
                        _logger.LogWarning(
                            "Rate limit exceeded for {GrantType} from {ClientIp} (client_id={ClientId}). " +
                            "Count={Count}, WindowStart={WindowStart:s}, MaxPerWindow={Max}",
                            grantType, clientIp, clientId ?? "unknown", window.Count, window.Start, _options.MaxRequestsPerWindow);

                        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                        context.Response.Headers["Retry-After"] = _options.WindowSeconds.ToString();
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = "rate_limit_exceeded",
                            error_description = $"Too many {grantType} requests. Try again in {_options.WindowSeconds} seconds."
                        });
                        return;
                    }
                }
            }
        }

        await _next(context);
    }
}

/// <summary>
/// A fixed-window counter that resets when the window expires.
/// </summary>
internal class RateWindow(DateTimeOffset start, int count = 0)
{
    private readonly object _lock = new();

    public DateTimeOffset Start { get; private set; } = start;
    public int Count { get; set; } = count;

    /// <summary>
    /// Advance the window to <paramref name="now"/> if the current window has expired.
    /// Thread-safe.
    /// </summary>
    public void TryAdvance(DateTimeOffset now, int windowSeconds)
    {
        lock (_lock)
        {
            if (now - Start >= TimeSpan.FromSeconds(windowSeconds))
            {
                Start = now;
                Count = 0;
            }
        }
    }
}

/// <summary>
/// Configuration for token endpoint rate limiting.
/// Bound from "TokenRateLimit" section.
/// </summary>
public class TokenRateLimitOptions
{
    /// <summary>
    /// Maximum requests per window per client IP per grant type.
    /// Default: 10 (per window).
    /// </summary>
    public int MaxRequestsPerWindow { get; set; } = 10;

    /// <summary>
    /// Window duration in seconds.
    /// Default: 60 (1 minute).
    /// </summary>
    public int WindowSeconds { get; set; } = 60;
}
