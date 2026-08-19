using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaindSoft.AdminUI.Services;

namespace TaindSoft.AdminUI.Extensions
{
    /// <summary>
    /// TODO: Document class AdminUIServiceCollectionExtensions
    /// </summary>
    public static class AdminUIServiceCollectionExtensions
    {
        public static IServiceCollection AddTaindSoftAdminUI(
            this IServiceCollection services,
            Action<AdminUIOptions>? configureOptions = null)
        {
            // Register core AdminUI services
            AdminModuleRegistry moduleRegistry = new();
            services.AddSingleton<IAdminModuleRegistry>(moduleRegistry);
            services.AddScoped<IAdminMenuService, AdminMenuService>();
            // Register toast service
            services.AddScoped<IAdminToastService, AdminToastService>();

            // Register sidebar state provider fallback (expanded by default).
            // Server-side hosts override this with a cookie-aware implementation.
            services.TryAddScoped<ISidebarStateProvider, DefaultSidebarStateProvider>();

            // Register breadcrumb label provider
            //services.AddSingleton<IBreadcrumbLabelProvider, AdminBreadcrumbDefaults>();

            // Configure options and discover modules
            if (configureOptions != null)
            {
                AdminUIOptions options = new();
                configureOptions(options);

                // ponytail: Use caller-supplied options if configured; otherwise default
                // (matches prior `AddAdminAuthentication()` no-op behavior). The duplicate
                // `AddAdminAuthentication(options.AuthenticationOptions)` that lived here
                // collided with the prior default registration on `AddSingleton(options)`.
                // Removed legacy authentication registration (host is gone)
// services.AddAdminAuthentication(options.AuthenticationOptions);

                // Discover and register modules from assemblies (automatic discovery when possible)
                AdminModuleDiscoveryService discoveryService = new(moduleRegistry, services);
                discoveryService.DiscoverModules(options);
            }
            else
            {
                // Removed legacy authentication registration (host is gone)
                // services.AddAdminAuthentication();
            }

            return services;
        }
    }
}
