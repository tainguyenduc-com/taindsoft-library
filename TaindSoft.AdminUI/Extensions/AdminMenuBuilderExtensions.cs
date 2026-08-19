using TaindSoft.AdminUI.Navigation;

namespace TaindSoft.AdminUI.Extensions
{
    /// <summary>
    /// Extension methods for IAdminMenuBuilder for easier menu item configuration
    /// </summary>
    public static class AdminMenuBuilderExtensions
    {
        /// <summary>
        /// Add a dashboard item (top-level, priority 0)
        /// </summary>
        public static IAdminMenuBuilder AddDashboard(
            this IAdminMenuBuilder builder,
            string url = "/admin",
            string icon = "🏠",
            string label = "Dashboard",
            string id = "dashboard")
        {
            return builder.AddTopLevelItem(id, label, url, icon, priority: 0);
        }

        /// <summary>
        /// Add a top-level item with default section settings
        /// </summary>
        public static IAdminMenuBuilder AddTopLevel(
            this IAdminMenuBuilder builder,
            string id,
            string label,
            string url,
            string icon,
            int priority = 10)
        {
            return builder.AddTopLevelItem(id, label, url, icon, priority);
        }

        /// <summary>
        /// Add an item to a specific section with convenient parameters
        /// </summary>
        public static IAdminMenuBuilder AddToSection(
            this IAdminMenuBuilder builder,
            string section,
            string id,
            string label,
            string url,
            string icon,
            int order = 0)
        {
            return builder.AddItem(id, label, url, icon, section, order);
        }

        /// <summary>
        /// Add multiple items to the same section
        /// </summary>
        public static IAdminMenuBuilder AddToSection(
            this IAdminMenuBuilder builder,
            string section,
            params (string id, string label, string url, string icon)[] items)
        {
            int order = 0;
            foreach ((string? id, string? label, string? url, string? icon) in items)
            {
                builder.AddItem(id, label, url, icon, section, order++);
            }
            return builder;
        }
    }
}
