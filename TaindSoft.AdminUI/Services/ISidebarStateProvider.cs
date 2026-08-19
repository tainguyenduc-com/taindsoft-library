namespace TaindSoft.AdminUI.Services;

/// <summary>
/// Provides the initial sidebar open/closed state from the persisted cookie.
/// Implemented by each host (server-side) which has access to HttpContext.
/// Falls back to true (expanded) when not registered or cookie is absent.
/// </summary>
public interface ISidebarStateProvider
{
    /// <summary>
    /// Returns true if the sidebar should start expanded, false if collapsed.
    /// </summary>
    bool GetDefaultOpen();
}

/// <summary>
/// Default implementation — always returns true (expanded).
/// Used in WASM client and any context without cookie access.
/// </summary>
public sealed class DefaultSidebarStateProvider : ISidebarStateProvider
{
    public bool GetDefaultOpen() => true;
}
