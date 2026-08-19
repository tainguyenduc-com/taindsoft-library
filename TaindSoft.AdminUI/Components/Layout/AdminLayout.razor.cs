using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TaindSoft.AdminUI.Services;

namespace TaindSoft.AdminUI.Components.Layout;

/// <summary>
/// Code-behind for AdminLayout — loads authenticated user info
/// and passes it to ShadcnProfileDropdown in the header.
/// Also reads the sidebar_state cookie via ISidebarStateProvider to restore
/// the correct open/closed state on SSR, eliminating the hydration mismatch.
/// </summary>
public partial class AdminLayout : LayoutComponentBase
{
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ISidebarStateProvider SidebarStateProvider { get; set; } = default!;

    private string _userName = "Admin";
    private string _userEmail = string.Empty;
    private string _userInitials = "AD";
    private string _avatarSrc = string.Empty;

    /// <summary>
    /// Sidebar open state read from the sidebar_state cookie via ISidebarStateProvider.
    /// Defaults to true (expanded) when no cookie is present.
    /// Read once during SSR so the initial render matches the persisted state.
    /// </summary>
    private bool _sidebarDefaultOpen = true;

    protected override async Task OnInitializedAsync()
    {
        // Read sidebar_state cookie to restore persisted state on SSR.
        _sidebarDefaultOpen = SidebarStateProvider.GetDefaultOpen();

        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var claims = authState?.User;

            _userName = claims?.FindFirst("name")?.Value
                        ?? claims?.Identity?.Name
                        ?? "Admin";

            _userEmail = claims?.FindFirst("email")?.Value
                         ?? claims?.Claims?.FirstOrDefault(c =>
                             c.Type.Contains("mail", StringComparison.OrdinalIgnoreCase))?.Value
                         ?? string.Empty;

            _avatarSrc = claims?.FindFirst("picture")?.Value
                         ?? claims?.FindFirst("avatar")?.Value
                         ?? string.Empty;

            _userInitials = BuildInitials(_userName);
        }
        catch
        {
            // Fallback — auth state not yet available (SSR pre-render)
            _userName = "Admin";
            _userInitials = "AD";
        }
    }

    private static string BuildInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "AD";
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}";
        var n = name.Trim();
        return n.Length >= 2
            ? $"{char.ToUpperInvariant(n[0])}{char.ToUpperInvariant(n[^1])}"
            : n.ToUpperInvariant();
    }

    private Task HandleSignOut()
    {
        NavigationManager.NavigateTo("/auth/sign-out", forceLoad: true);
        return Task.CompletedTask;
    }
}
