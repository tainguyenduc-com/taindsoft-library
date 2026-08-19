using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace TaindSoft.Core.Localization.Extensions
{
    /// <summary>
    /// Extension methods for registering localization infrastructure
    /// Does NOT provide resources - each module manages its own resource files
    /// </summary>
    public static class LocalizationServiceExtensions
    {
        /// <summary>
        /// Register localization infrastructure (middleware, culture providers)
        /// Modules are responsible for their own resource files
        /// </summary>
        public static IServiceCollection AddCoreLocalization(
            this IServiceCollection services,
            string defaultCulture = "vi-VN",
            params string[] supportedCultures)
        {
            string[] cultures = supportedCultures.Length > 0
                ? supportedCultures
                : ["en-US", "vi-VN"];

            // Register localization infrastructure WITHOUT specifying ResourcesPath
            // Modules will configure their own ResourcesPath in their registration
            services.AddLocalization();

            services.Configure<RequestLocalizationOptions>(options =>
            {
                List<CultureInfo> supportedCulturesList = cultures
                    .Select(c => new CultureInfo(c))
                    .ToList();

                options.DefaultRequestCulture = new RequestCulture(defaultCulture);
                options.SupportedCultures = supportedCulturesList;
                options.SupportedUICultures = supportedCulturesList;

                // Culture detection priority: Query String > Accept-Language Header > Cookie
                options.RequestCultureProviders =
                [
                    new QueryStringRequestCultureProvider(), // ?culture=vi-VN
                    new AcceptLanguageHeaderRequestCultureProvider(), // Accept-Language: vi-VN
                    new CookieRequestCultureProvider() // Cookie: .AspNetCore.Culture
                ];
            });

            return services;
        }

        /// <summary>
        /// Use request localization middleware
        /// Must be called early in the pipeline (before MVC/endpoints)
        /// </summary>
        public static IApplicationBuilder UseCoreLocalization(this IApplicationBuilder app)
        {
            app.UseRequestLocalization();
            return app;
        }
    }
}
