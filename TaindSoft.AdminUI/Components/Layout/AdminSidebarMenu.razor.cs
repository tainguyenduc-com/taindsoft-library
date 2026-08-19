#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Shadcn.Admin.Components.Layout;
using Shadcn.Admin.Lib;
using TaindSoft.AdminUI.Navigation;
using TaindSoft.AdminUI.Services;

namespace TaindSoft.AdminUI.Components.Layout;

/// <summary>
/// AdminSidebarMenu — sidebar composition rendered inside ShadcnAuthenticatedLayout's
/// ShadcnSidebar. Maps AdminMenuItem from service → ShadcnNavGroup.NavItem with icons.
/// Supports top-level items (General) and grouped sections.
/// Includes workspace switcher (data-driven) and mobile auto-close on navigation.
/// </summary>
public partial class AdminSidebarMenu
{
    private List<AdminMenuItem> _topLevelItems = new();
    private Dictionary<string, List<AdminMenuItem>> _menuBySection = new();
    private ShadcnNavUser.UserItem? _user;
    private AdminWorkspaceInfo? _workspace;

    [Inject] private IAdminMenuService AdminMenuService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    
    [CascadingParameter] private SidebarContext? SidebarCtx { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // Load workspace info for sidebar header
        _workspace = await AdminMenuService.GetCurrentWorkspaceAsync();
        
        // Load menu items from service
        _topLevelItems = await AdminMenuService.GetTopLevelMenuAsync();
        _menuBySection = await AdminMenuService.GetMenuBySection();

        // Load authenticated user info for footer
        await LoadUserAsync();
    }

    /// <summary>
    /// Convert AdminMenuItem list to ShadcnNavGroup.NavItem list.
    /// Maps Icon (CSS class or SVG) and Badge.
    /// </summary>
    private IReadOnlyList<ShadcnNavGroup.NavItem> MapToNavItems(List<AdminMenuItem> items)
    {
        return items.Select(item => new ShadcnNavGroup.NavItem
        {
            Title = item.Label,
            Url = item.Url,
            Badge = null, // Badge placeholder; can be extended per module
            Icon = RenderIcon(item.Icon)
        }).ToList();
    }

    /// <summary>
    /// Render icon as RenderFragment.
    /// Supports Lucide icons only (no Bootstrap legacy).
    /// Falls back to generic dot icon if icon name is not found.
    /// </summary>
    private RenderFragment? RenderIcon(string? iconInput)
    {
        // Only Lucide icons: if it's a valid Lucide name, use LucideIconSvg
        if (!string.IsNullOrWhiteSpace(iconInput))
        {
            var lucideIcon = LucideIconSvg.GetIcon(iconInput);
            if (lucideIcon is not null)
                return lucideIcon;
        }

        // Default fallback: generic dot icon from Lucide
        return LucideIconSvg.GetIcon("dot") ?? new RenderFragment(b => b.AddContent(0, "•"));
    }

    /// <summary>
    /// Load authenticated user from auth state provider.
    /// Extracts name, email, and avatar from JWT claims.
    /// Falls back to default user if auth fails.
    /// </summary>
    private async Task LoadUserAsync()
    {
        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var claims = authState?.User;

            _user = new ShadcnNavUser.UserItem
            {
                Name = claims?.FindFirst("name")?.Value
                       ?? claims?.Identity?.Name
                       ?? "Admin",
                Email = claims?.FindFirst("email")?.Value
                        ?? claims?.Claims?.FirstOrDefault(c => c.Type.Contains("mail", StringComparison.OrdinalIgnoreCase))?.Value
                        ?? "",
                // Avatar: look for "picture", "avatar", or "image" claims
                Avatar = claims?.FindFirst("picture")?.Value
                         ?? claims?.FindFirst("avatar")?.Value
                         ?? claims?.FindFirst("image")?.Value
            };
        }
        catch
        {
            // Fallback if auth fails
            _user = new ShadcnNavUser.UserItem
            {
                Name = "Admin",
                Email = "",
                Avatar = null
            };
        }
    }

    private Task HandleSignOut()
    {
        // Navigate to sign-out endpoint with force reload to clear session
        NavigationManager.NavigateTo("/auth/sign-out", true);
        return Task.CompletedTask;
    }

    private Task HandleWorkspaceSelect()
    {
        // Close sidebar on mobile after workspace selection
        if (SidebarCtx?.IsMobile == true)
        {
            SidebarCtx.SetOpenMobile(false);
        }
        return Task.CompletedTask;
    }
}
