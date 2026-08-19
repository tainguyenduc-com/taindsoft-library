using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics;
using TaindSoft.Core.Localization.Abstractions;

namespace TaindSoft.Core.Localization
{
    /// <summary>
    /// Extension methods for registering localization services
    /// </summary>
    public static class LocalizationServiceCollectionExtensions
    {
        /// <summary>
        /// Adds localization services with default configuration
        /// </summary>
        /// <remarks>
        /// By default, supports English (en) and Vietnamese (vi).
        /// Resources should be in "Resources" folder with format: {culture}.json
        /// </remarks>
        /// <param name="services">The service collection</param>
        /// <param name="supportedCultures">Supported culture codes (default: "en", "vi")</param>
        /// <param name="defaultCulture">Default culture if negotiation fails (default: "en")</param>
        /// <param name="resourcesPath">Path to resources folder (default: "Resources")</param>
        /// <returns>The service collection for chaining</returns>
        /// <example>
        /// <code>
        /// builder.Services.AddLocalization(
        ///     supportedCultures: new[] { "en", "vi", "es" },
        ///     defaultCulture: "en",
        ///     resourcesPath: "i18n"
        /// );
        /// </code>
        /// </example>
        public static IServiceCollection AddLocalization(
            this IServiceCollection services,
            string[]? supportedCultures = null,
            string defaultCulture = "en",
            string resourcesPath = "Resources")
        {
            ArgumentNullException.ThrowIfNull(services);

            supportedCultures ??= ["en", "vi"];

            if (!supportedCultures.Contains(defaultCulture))
            {
                throw new ArgumentException(
                    $"Default culture '{defaultCulture}' must be in supported cultures list",
                    nameof(defaultCulture));
            }

            // Register culture provider
            services.TryAddScoped<ICultureProvider>(sp =>
            {
                IHttpContextAccessor? httpContextAccessor = sp.GetService<IHttpContextAccessor>();
                return new CultureProvider(httpContextAccessor, supportedCultures, defaultCulture);
            });

            // Register resource manager
            services.TryAddSingleton<IResourceManager>(sp =>
            {
                JsonResourceManager manager = new(resourcesPath);

                // Pre-load resources
                foreach (string culture in supportedCultures)
                {
                    try
                    {
                        string filePath = Path.Combine(resourcesPath, $"{culture}.json");
                        if (File.Exists(filePath))
                        {
                            manager.LoadResourcesAsync(culture, filePath).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to load resources for culture '{culture}': {ex.Message}");
                    }
                }

                return manager;
            });

            // Register string localizers
            services.TryAddScoped<IStringLocalizer>(sp =>
            {
                IResourceManager resourceManager = sp.GetRequiredService<IResourceManager>();
                ICultureProvider cultureProvider = sp.GetRequiredService<ICultureProvider>();
                return new StringLocalizer(resourceManager, cultureProvider);
            });

            // Register generic string localizer factory
            services.TryAddScoped(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));

            // Register HTTP context accessor if not already registered
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            return services;
        }

        /// <summary>
        /// Adds localization with custom resource loader
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="resourceManagerFactory">Factory to create custom resource manager</param>
        /// <param name="supportedCultures">Supported culture codes</param>
        /// <param name="defaultCulture">Default culture</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddLocalization(
            this IServiceCollection services,
            Func<IServiceProvider, IResourceManager> resourceManagerFactory,
            string[]? supportedCultures = null,
            string defaultCulture = "en")
        {
            ArgumentNullException.ThrowIfNull(services);

            ArgumentNullException.ThrowIfNull(resourceManagerFactory);

            supportedCultures ??= ["en", "vi"];

            // Register culture provider
            services.TryAddScoped<ICultureProvider>(sp =>
            {
                IHttpContextAccessor? httpContextAccessor = sp.GetService<IHttpContextAccessor>();
                return new CultureProvider(httpContextAccessor, supportedCultures, defaultCulture);
            });

            // Register custom resource manager
            services.TryAddSingleton(resourceManagerFactory);

            // Register string localizers
            services.TryAddScoped<IStringLocalizer>(sp =>
            {
                IResourceManager resourceManager = sp.GetRequiredService<IResourceManager>();
                ICultureProvider cultureProvider = sp.GetRequiredService<ICultureProvider>();
                return new StringLocalizer(resourceManager, cultureProvider);
            });

            services.TryAddScoped(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            return services;
        }
    }
}
