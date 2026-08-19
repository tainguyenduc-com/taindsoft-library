namespace TaindSoft.AdminUI.Navigation
{
    /// <summary>
    /// Builder interface for constructing admin navigation menus
    /// </summary>
    public interface IAdminMenuBuilder
    {
        /// <summary>
        /// Add a menu item
        /// </summary>
        IAdminMenuBuilder AddItem(AdminMenuItem item);

        /// <summary>
        /// Add multiple menu items
        /// </summary>
        IAdminMenuBuilder AddItems(IEnumerable<AdminMenuItem> items);

        /// <summary>
        /// Add a menu item using fluent API
        /// </summary>
        IAdminMenuBuilder AddItem(string id, string label, string url, string icon, string section, int order);

        /// <summary>
        /// Add a top-level menu item (Dashboard, outside any section)
        /// </summary>
        IAdminMenuBuilder AddTopLevelItem(string id, string label, string url, string icon, int priority = 0);

        /// <summary>
        /// Build the final menu structure
        /// </summary>
        List<AdminMenuItem> Build();
    }

    /// <summary>
    /// Default implementation of admin menu builder.
    /// Transforms /admin/... URLs to use the configured prefix when non-default.
    /// </summary>
    public class AdminMenuBuilder : IAdminMenuBuilder
    {
        private readonly List<AdminMenuItem> _items = [];
        private readonly string _prefix;

        /// <summary>
        /// Creates a new AdminMenuBuilder with optional prefix.
        /// </summary>
        /// <param name="prefix">URL prefix for admin pages (default "admin").
        /// When set to e.g. "backoffice", menu URLs /admin/... become /backoffice/...</param>
        public AdminMenuBuilder(string prefix = "admin")
        {
            _prefix = prefix.Trim('/');
        }

        public IAdminMenuBuilder AddItem(AdminMenuItem item)
        {
            item.Url = TransformUrl(item.Url);
            _items.Add(item);
            return this;
        }

        public IAdminMenuBuilder AddItems(IEnumerable<AdminMenuItem> items)
        {
            foreach (var item in items)
            {
                item.Url = TransformUrl(item.Url);
                _items.Add(item);
            }
            return this;
        }

        public IAdminMenuBuilder AddItem(string id, string label, string url, string icon, string section, int order)
        {
            return AddItem(new AdminMenuItem
            {
                Id = id,
                Label = label,
                Url = url,
                Icon = icon,
                Section = section,
                Order = order,
                Priority = 1000 // Normal priority
            });
        }

        public IAdminMenuBuilder AddTopLevelItem(string id, string label, string url, string icon, int priority = 0)
        {
            return AddItem(new AdminMenuItem
            {
                Id = id,
                Label = label,
                Url = url,
                Icon = icon,
                Section = null,
                Order = 0,
                Priority = priority
            });
        }

        public List<AdminMenuItem> Build()
        {
            return [.. _items
                .OrderBy(i => i.Priority)      // Priority first (0 = highest, displays first)
                .ThenBy(i => i.Section)        // Then by section
                .ThenBy(i => i.Order)];
        }

        /// <summary>
        /// Transforms /admin/... to /{prefix}/... when prefix is not "admin".
        /// Non-admin URLs pass through unchanged.
        /// </summary>
        private string TransformUrl(string url)
        {
            if (string.IsNullOrEmpty(url) || _prefix == "admin")
            {
                return url;
            }

            var normalized = url.StartsWith('/') ? url : "/" + url;

            // /admin/something → /{prefix}/something
            if (normalized.StartsWith("/admin/", StringComparison.OrdinalIgnoreCase))
            {
                return "/" + _prefix + normalized[6..];
            }

            // /admin → /{prefix}
            if (string.Equals(normalized, "/admin", StringComparison.OrdinalIgnoreCase))
            {
                return "/" + _prefix;
            }

            return url;
        }
    }

}
