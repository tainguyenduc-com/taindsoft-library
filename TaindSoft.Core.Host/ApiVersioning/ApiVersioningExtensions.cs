using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace TaindSoft.Core.Host.ApiVersioning
{
    /// <summary>
    /// Extension methods for API versioning configuration
    /// </summary>
    public static class ApiVersioningExtensions
    {
        /// <summary>
        /// Configure API versioning with header-based version routing
        /// Supports: X-API-Version header or url-based versioning
        /// </summary>
        public static IServiceCollection AddApiVersioningConfiguration(
            this IServiceCollection services,
            bool useUrlSegmentVersioning = false)
        {
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;

                if (useUrlSegmentVersioning)
                {
                    options.ApiVersionReader = new UrlSegmentApiVersionReader();
                }
                else
                {
                    // Use header-based versioning: X-API-Version: 1.0
                    options.ApiVersionReader = new HeaderApiVersionReader("X-API-Version");
                }

                // options.ErrorResponses = new ApiVersioningErrorResponseProvider();
            });

            return services;
        }
    }
}
