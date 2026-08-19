namespace TaindSoft.Core.Host.Security;

/// <summary>
/// Logs authentication events (login, token issue, revoke, refresh) for audit trail.
/// Implemented at host level where both Core.Host and AuditLog feature are referenced.
/// </summary>
public interface IAuthenticationAuditLogger
{
    /// <summary>
    /// Log a successful user login.
    /// </summary>
    Task LogLoginAsync(string userId, string? userName, string? ipAddress, string? userAgent);

    /// <summary>
    /// Log a failed login attempt.
    /// </summary>
    Task LogLoginFailedAsync(string? email, string? ipAddress, string reason);

    /// <summary>
    /// Log token issuance (access + refresh token).
    /// </summary>
    Task LogTokenIssuedAsync(string userId, string? userName, string tokenType, string? ipAddress);

    /// <summary>
    /// Log token revocation.
    /// </summary>
    Task LogTokenRevokedAsync(string userId, string? userName, string tokenType, string? ipAddress);

    /// <summary>
    /// Log token refresh.
    /// </summary>
    Task LogTokenRefreshedAsync(string userId, string? userName, string? ipAddress);
}
