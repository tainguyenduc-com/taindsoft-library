using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using TaindSoft.Core.Configuration;

namespace TaindSoft.Core.Config
{
    /// <summary>
    /// TODO: Document class ServiceCollectionExtensions
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers Http-based IConfigProvider which calls SystemManagement service.
        /// Expects configuration key "SystemManagement:BaseUrl" for remote base URL.
        /// </summary>
        public static IServiceCollection AddCoreConfig(this IServiceCollection services, IConfiguration configuration)
        {
            // Validate critical configuration first (will throw outside Development env if missing)
            configuration.EnsureRequired("SystemManagement:BaseUrl", "SystemManagement:BaseAddress");

            string baseUrl = configuration["SystemManagement:BaseUrl"] ?? configuration["SystemManagement:BaseAddress"] ?? "http://localhost:7003";

            services.AddHttpClient<HttpConfigProvider>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            });

            services.AddScoped<IConfigProvider, HttpConfigProvider>();

            // Ensure JsonSerializerOptions is available
            services.AddSingleton(provider => new JsonSerializerOptions(JsonSerializerDefaults.Web));

            return services;
        }
    }
}
