using System.Reflection;

using TaindSoft.AdminUI.Navigation;

namespace TaindSoft.AdminUI
{
    /// <summary>
    /// Options for configuring the TaindSoft Admin UI framework
    /// </summary>
    /// <summary>
    /// Configuration options for the Admin UI library (UI behavior and feature toggles).
    /// </summary>
    public class AdminUIOptions
    {
        /// <summary>
        /// Additional menu items provided by the host application
        /// </summary>
        public List<AdminMenuItem> AdditionalMenuItems { get; } = [];

        /// <summary>
        /// Module assemblies to discover and load
        /// </summary>
        public List<Assembly> ModuleAssemblies { get; } = [];

        /// <summary>
        /// API base URL for module services
        /// </summary>
        public string ApiBaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Authentication configuration
        /// </summary>
        public object? AuthenticationOptions { get; set; } // Placeholder for removal of dependency


        /// <summary>
        /// Add a top-level menu item (e.g., Dashboard - no section)
        /// </summary>
        /// <param name="id">Unique identifier</param>
        /// <param name="label">Display label</param>
        /// <param name="url">Navigation URL</param>
        /// <param name="icon">Icon CSS class or emoji</param>
        /// <param name="priority">Display priority (0 = highest, displays first)</param>
        public AdminUIOptions AddTopLevelMenuItem(string id, string label, string url, string icon, int priority = 0)
        {
            AdditionalMenuItems.Add(new AdminMenuItem
            {
                Id = id,
                Label = label,
                Url = url,
                Icon = icon,
                Section = null,
                Order = 0,
                Priority = priority
            });

            return this;
        }

        /// <summary>
        /// Add a sectioned menu item (grouped under a category)
        /// </summary>
        /// <param name="id">Unique identifier</param>
        /// <param name="label">Display label</param>
        /// <param name="url">Navigation URL</param>
        /// <param name="icon">Icon CSS class or emoji</param>
        /// <param name="section">Section/category name</param>
        /// <param name="order">Display order within section</param>
        public AdminUIOptions AddSectionedMenuItem(string id, string label, string url, string icon, string section, int order = 0)
        {
            AdditionalMenuItems.Add(new AdminMenuItem
            {
                Id = id,
                Label = label,
                Url = url,
                Icon = icon,
                Section = section,
                Order = order,
                Priority = 1000 // Normal priority for sectioned items
            });

            return this;
        }

        /// <summary>
        /// Add a raw menu item
        /// </summary>
        public AdminUIOptions AddMenuItem(AdminMenuItem item)
        {
            AdditionalMenuItems.Add(item);
            return this;
        }

        /// <summary>
        /// Add multiple custom menu items
        /// </summary>
        public AdminUIOptions AddMenuItems(params AdminMenuItem[] items)
        {
            AdditionalMenuItems.AddRange(items);
            return this;
        }

        /// <summary>
        /// Add module assembly for discovery
        /// </summary>
        public AdminUIOptions AddModuleAssembly(string assemblyName)
        {
            try
            {
                Assembly assembly = Assembly.Load(assemblyName);
                ModuleAssemblies.Add(assembly);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load assembly '{assemblyName}'", ex);
            }

            return this;
        }

        /// <summary>
        /// Add module assembly by Assembly object
        /// </summary>
        public AdminUIOptions ScanModulesFromAssemblies(params Assembly[] assemblies)
        {
            if (assemblies != null && assemblies.Length > 0)
            {
                ModuleAssemblies.AddRange(assemblies);
            }
            return this;
        }

        /// <summary>
        /// Configure API base URL for module services
        /// </summary>
        public AdminUIOptions WithApiBaseUrl(string apiBaseUrl)
        {
            ApiBaseUrl = apiBaseUrl;
            return this;
        }

        /// <summary>
        /// Enable authentication for the admin UI
        /// </summary>
        public AdminUIOptions WithAuthentication(Action<object>? configure = null)
        {
            AuthenticationOptions = new object(); // Placeholder
            configure?.Invoke(AuthenticationOptions);
            return this;
        }
    }
}
