using Microsoft.Extensions.DependencyInjection;
using TaindSoft.AdminUI.Navigation;

namespace TaindSoft.AdminUI.Contracts
{
    /// <summary>
    /// Contract that all admin modules must implement to register with the admin framework
    /// </summary>
    public interface IAdminModule
    {
        /// <summary>
        /// Unique module name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Module display name shown in UI
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Module version
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Module dependencies - other modules that must be loaded before this one
        /// </summary>
        string[] Dependencies { get; }

        /// <summary>
        /// Configure module-specific services
        /// </summary>
        void ConfigureServices(IServiceCollection services, string apiBaseUrl);

        /// <summary>
        /// Configure navigation menu items
        /// </summary>
        void ConfigureNavigation(IAdminMenuBuilder menuBuilder);
    }
}
