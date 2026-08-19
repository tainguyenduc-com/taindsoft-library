using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using TaindSoft.Core.Mapping.Abstractions;

namespace TaindSoft.Core.Mapping
{
    /// <summary>
    /// Extension methods for registering object mapping services
    /// </summary>
    public static class MappingServiceCollectionExtensions
    {
        public static IServiceCollection AddObjectMapping(
            this IServiceCollection services,
            params Assembly[] assemblies)
        {
            return services.AddObjectMapping(typeFilter: null, assemblies);
        }

        /// <summary>
        /// Adds object mapping services to the dependency injection container
        /// </summary>
        /// <remarks>
        /// This method automatically discovers and registers all mapping profiles
        /// that inherit from IMappingProfile in the specified assemblies.
        /// </remarks>
        /// <param name="services">The service collection</param>
        /// <param name="typeFilter">Optional filter to apply when scanning for mapping profiles. Defaults to including all concrete classes.</param>
        /// <param name="assemblies">The assemblies to scan for mapping profiles</param>
        /// <returns>The service collection for chaining</returns>
        /// <example>
        /// <code>
        /// builder.Services.AddObjectMapping(typeof(Program).Assembly);
        /// 
        /// // Or with multiple assemblies
        /// builder.Services.AddObjectMapping(
        ///     typeof(Program).Assembly,
        ///     typeof(UserProfile).Assembly
        /// );
        /// </code>
        /// </example>
        public static IServiceCollection AddObjectMapping(
            this IServiceCollection services,
            Func<Type, bool>? typeFilter = null,
            params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
            {
                throw new ArgumentException("At least one assembly must be provided", nameof(assemblies));
            }

            // Register the object mapper
            services.TryAddScoped<IObjectMapper, ObjectMapper>();

            // Discover and register mapping profiles
            Type profileType = typeof(IMappingProfile);
            List<Type> profiles = new();

            Func<Type, bool> filter = typeFilter ?? (t => t.IsClass && !t.IsAbstract);

            foreach (Assembly assembly in assemblies)
            {
                List<Type> assemblyProfiles = assembly.GetTypes()
                    .Where(t => profileType.IsAssignableFrom(t) && filter(t) && !t.IsInterface)
                    .ToList();

                profiles.AddRange(assemblyProfiles);
            }

            // Optionally: Store profiles for later initialization
            // This could be used to lazily initialize custom mappings
            foreach (Type profile in profiles)
            {
                services.TryAddScoped(profileType, profile);
            }

            return services;
        }

        /// <summary>
        /// Configures and builds all registered mapping profiles.
        /// Call this after AddObjectMapping to execute profile configuration at startup.
        /// </summary>
        public static IServiceCollection ConfigureMappings(this IServiceCollection services)
        {
            using (var sp = services.BuildServiceProvider())
            {
                var config = new MappingConfiguration();
                foreach (var profile in sp.GetServices<IMappingProfile>())
                {
                    profile.Configure(config);
                }
            }
            return services;
        }

        /// <summary>
        /// Adds object mapping services with a custom mapper factory
        /// </summary>
        /// <remarks>
        /// Use this overload if you want to provide a custom IObjectMapper implementation
        /// or configure the mapper with specific options.
        /// </remarks>
        /// <param name="services">The service collection</param>
        /// <param name="factory">A factory function to create the object mapper</param>
        /// <param name="assemblies">The assemblies to scan for mapping profiles</param>
        /// <param name="typeFilter">Optional filter to apply when scanning for mapping profiles. Defaults to including all concrete classes.</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddObjectMapping(
            this IServiceCollection services,
            Func<IServiceProvider, IObjectMapper> factory,
            Func<Type, bool>? typeFilter = null,
            params Assembly[] assemblies)
        {
            ArgumentNullException.ThrowIfNull(factory);

            // Register with custom factory
            services.TryAddScoped(factory);

            // Discover and register mapping profiles
            Type profileType = typeof(IMappingProfile);

            Func<Type, bool> filter = typeFilter ?? (t => t.IsClass && !t.IsAbstract);

            foreach (Assembly assembly in assemblies)
            {
                List<Type> profiles = assembly.GetTypes()
                    .Where(t => profileType.IsAssignableFrom(t) && filter(t) && !t.IsInterface)
                    .ToList();

                foreach (Type profile in profiles)
                {
                    services.TryAddScoped(profileType, profile);
                }
            }

            return services;
        }
    }
}
