using TaindSoft.AdminUI.Contracts;
using TaindSoft.AdminUI.Navigation;

namespace TaindSoft.AdminUI.Services;

/// <summary>
/// Workspace/team information for sidebar header (TeamSwitcher)
/// </summary>
public class AdminWorkspaceInfo
{
    public string Name { get; set; } = "TaindSoft Admin";
    public string Plan { get; set; } = "Pro";
}

/// <summary>
/// Service that aggregates navigation from all registered modules
/// </summary>
public interface IAdminMenuService
{
    /// <summary>
    /// Get the complete navigation menu from all modules
    /// </summary>
    Task<List<AdminMenuItem>> GetMenuAsync();

    /// <summary>
    /// Get only top-level menu items (outside any section, e.g., Dashboard)
    /// </summary>
    Task<List<AdminMenuItem>> GetTopLevelMenuAsync();

    /// <summary>
    /// Get menu items grouped by section
    /// </summary>
    Task<Dictionary<string, List<AdminMenuItem>>> GetMenuBySection();

    /// <summary>
    /// Get current workspace/team info for sidebar header
    /// </summary>
    Task<AdminWorkspaceInfo> GetCurrentWorkspaceAsync();

    /// <summary>
    /// Clear the navigation cache
    /// </summary>
    void ClearCache();
}

/// <summary>
/// Contract for modules that need to contribute menu items dynamically at runtime.
/// </summary>
public interface IAdminDynamicMenuProvider
{
    Task<IEnumerable<AdminMenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of admin menu service with caching
/// </summary>
public class AdminMenuService(
    IEnumerable<IAdminModule> modules,
    IEnumerable<IAdminDynamicMenuProvider> dynamicMenuProviders) : IAdminMenuService
{
    private readonly IEnumerable<IAdminModule> _modules = modules;
    private readonly IEnumerable<IAdminDynamicMenuProvider> _dynamicMenuProviders = dynamicMenuProviders;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private List<AdminMenuItem>? _cachedMenu;
    private DateTime? _cacheTime;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    private static readonly Dictionary<string, int> SectionOrder = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Content"] = 10,
        ["Customer"] = 20,
        ["Identity"] = 30,
        ["Management"] = 40,
        ["System"] = 50
    };

    public async Task<List<AdminMenuItem>> GetMenuAsync()
    {
        return await GetMenuInternalAsync();
    }

    public async Task<List<AdminMenuItem>> GetTopLevelMenuAsync()
    {
        List<AdminMenuItem> allMenu = await GetMenuInternalAsync();
        return [.. allMenu
            .Where(i => string.IsNullOrEmpty(i.Section))
            .OrderBy(i => i.Priority)];
    }

    public async Task<Dictionary<string, List<AdminMenuItem>>> GetMenuBySection()
    {
        List<AdminMenuItem> allMenu = await GetMenuInternalAsync();
        return allMenu
            .Where(i => !string.IsNullOrEmpty(i.Section))
            .GroupBy(i => i.Section ?? string.Empty)
            .OrderBy(g => GetSectionSort(g.Key))
            .ThenBy(g => g.Key)
            .ToDictionary(
                g => g.Key!,
                g => g
                    .OrderBy(i => IsSettingsLike(i) ? 1 : 0)
                    .ThenBy(i => i.Order)
                    .ThenBy(i => i.Label)
                    .ToList());
    }

    public async Task<AdminWorkspaceInfo> GetCurrentWorkspaceAsync()
    {
        // For now, return default workspace info
        // In future, this can be extended to support multi-tenancy
        return await Task.FromResult(new AdminWorkspaceInfo
        {
            Name = "TaindSoft Admin",
            Plan = "Pro"
        });
    }

    private static int GetSectionSort(string? section)
    {
        if (!string.IsNullOrWhiteSpace(section) && SectionOrder.TryGetValue(section, out int order))
        {
            return order;
        }

        return 1000;
    }

    private static bool IsSettingsLike(AdminMenuItem item)
    {
        string label = item.Label ?? string.Empty;
        string url = item.Url ?? string.Empty;

        return label.Contains("setting", StringComparison.OrdinalIgnoreCase)
            || url.Contains("/settings", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<List<AdminMenuItem>> GetMenuInternalAsync()
    {
        // Check if cache is valid
        if (_cachedMenu != null && _cacheTime.HasValue
            && DateTime.UtcNow - _cacheTime.Value < _cacheExpiration)
        {
            return _cachedMenu;
        }

        await _cacheLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_cachedMenu != null && _cacheTime.HasValue
                && DateTime.UtcNow - _cacheTime.Value < _cacheExpiration)
            {
                return _cachedMenu;
            }

            AdminMenuBuilder builder = new();

            // Let each module contribute its static menu items
            foreach (IAdminModule module in _modules)
            {
                module.ConfigureNavigation(builder);
            }

            // Let providers contribute dynamic menu items (for example, menu from API data).
            foreach (IAdminDynamicMenuProvider provider in _dynamicMenuProviders)
            {
                try
                {
                    IEnumerable<AdminMenuItem> items = await provider.GetMenuItemsAsync();
                    builder.AddItems(items);
                }
                catch
                {
                    // Ignore provider failures to keep sidebar usable.
                }
            }

            List<AdminMenuItem> allMenuItems = builder.Build();

            // Final sort: Priority (top-level first), then section, then order
            _cachedMenu = [.. allMenuItems
                .GroupBy(i => i.Id)
                .Select(g => g.First())
                .OrderBy(i => i.Priority)
                .ThenBy(i => i.Section)
                .ThenBy(i => i.Order)];

            _cacheTime = DateTime.UtcNow;

            return _cachedMenu;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public void ClearCache()
    {
        // CA1416: SemaphoreSlim.Wait() is not supported on browser, use async version
        _cacheLock.WaitAsync().GetAwaiter().GetResult();
        try
        {
            _cachedMenu = null;
            _cacheTime = null;
        }
        finally
        {
            _cacheLock.Release();
        }
    }
}
