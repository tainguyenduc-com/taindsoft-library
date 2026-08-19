using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaindSoft.Core.Host.Security;

namespace TaindSoft.Core.Host.Middleware;

/// <summary>
/// Middleware that intercepts authentication-related endpoints (/connect/token, /connect/revoke)
/// and logs audit events via <see cref="IAuthenticationAuditLogger"/>.
/// Does NOT capture response body — inspects status code and request metadata only.
/// Place after UseRouting / UseAuthentication.
/// </summary>
public class AuthenticationAuditMiddleware(
    RequestDelegate next,
    ILogger<AuthenticationAuditMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<AuthenticationAuditMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        // Read form for /connect/token POST before downstream middleware consumes it
        string? grantType = null;
        string? clientId = null;
        string? username = null;

        bool isTokenEndpoint = context.Request.Method == HttpMethods.Post
            && context.Request.Path.StartsWithSegments("/connect/token", StringComparison.OrdinalIgnoreCase);

        bool isRevokeEndpoint = context.Request.Method == HttpMethods.Post
            && context.Request.Path.StartsWithSegments("/connect/revoke", StringComparison.OrdinalIgnoreCase);

        if ((isTokenEndpoint || isRevokeEndpoint) && context.Request.HasFormContentType)
        {
            var form = await context.Request.ReadFormAsync();
            grantType = form["grant_type"].ToString();
            clientId = form["client_id"].ToString();
            if (isTokenEndpoint)
            {
                username = form["username"].ToString();
            }
        }

        await _next(context);

        // Post-response audit logging (status code is available here)
        int statusCode = context.Response.StatusCode;
        string ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        string userAgent = context.Request.Headers.UserAgent.ToString();

        if (isTokenEndpoint)
        {
            var auditLogger = context.RequestServices.GetService<IAuthenticationAuditLogger>();

            if (statusCode == 200)
            {
                string? userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                string? userName = context.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                                   ?? context.User?.FindFirst("username")?.Value;

                if (auditLogger != null)
                {
                    switch (grantType)
                    {
                        case "authorization_code":
                        case "password":
                            await auditLogger.LogLoginAsync(
                                userId ?? username ?? "unknown", userName, ipAddress, userAgent);
                            await auditLogger.LogTokenIssuedAsync(
                                userId ?? username ?? "unknown", userName, "access_token+refresh_token", ipAddress);
                            break;
                        case "refresh_token":
                            await auditLogger.LogTokenRefreshedAsync(
                                userId ?? username ?? "unknown", userName, ipAddress);
                            break;
                    }
                }

                _logger.LogInformation(
                    "Token issued: grant_type={GrantType}, client_id={ClientId}, user={User}, ip={Ip}",
                    grantType, clientId, userId ?? username, ipAddress);
            }
            else if (statusCode is 400 or 401)
            {
                if (auditLogger != null && username != null)
                {
                    await auditLogger.LogLoginFailedAsync(username, ipAddress, "invalid_grant");
                }

                _logger.LogWarning(
                    "Token request failed: grant_type={GrantType}, client_id={ClientId}, user={User}, ip={Ip}, status={Status}",
                    grantType, clientId, username ?? "unknown", ipAddress, statusCode);
            }
        }

        if (isRevokeEndpoint && statusCode == 200)
        {
            var auditLogger = context.RequestServices.GetService<IAuthenticationAuditLogger>();
            string? userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            string? userName = context.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

            if (auditLogger != null && userId != null)
            {
                await auditLogger.LogTokenRevokedAsync(userId, userName, "refresh_token", ipAddress);
            }

            _logger.LogInformation("Token revoked: user={UserId}, ip={Ip}", userId, ipAddress);
        }
    }
}
