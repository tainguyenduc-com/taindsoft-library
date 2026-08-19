namespace TaindSoft.Core.Identity.Configuration;

/// <summary>
/// Configuration for session/auth absolute timeout.
/// Bound from "Session" configuration section.
/// </summary>
public class SessionTimeoutOptions
{
    /// <summary>
    /// Absolute maximum session lifetime (in hours).
    /// The session is forcefully terminated after this duration regardless of activity.
    /// Default: 24 hours.
    /// </summary>
    public int AbsoluteTimeoutHours { get; set; } = 24;

    /// <summary>
    /// Whether to log session expiry events.
    /// Default: true.
    /// </summary>
    public bool LogSessionExpiry { get; set; } = true;
}
