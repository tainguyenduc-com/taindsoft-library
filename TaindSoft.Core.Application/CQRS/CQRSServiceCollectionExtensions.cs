using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using TaindSoft.Core.Application.CQRS.Commands;
using TaindSoft.Core.Application.CQRS.CQRSPipelines;
using TaindSoft.Core.Application.CQRS.Queries;
using TaindSoft.Core.Application.Validation;

namespace TaindSoft.Core.Application.CQRS
{
    /// <summary>
    /// Extension methods for registering pure CQRS (no MediatR)
    /// </summary>
    public static class CQRSServiceCollectionExtensions
    {
        /// <summary>
        /// Add pure CQRS support
        /// </summary>
        public static IServiceCollection AddCQRS(
            this IServiceCollection services,
            params Assembly[] assemblies)
        {
            return services.AddCQRS(typeFilter: null, assemblies);
        }

        /// <summary>
        /// Add pure CQRS support with optional type filter for handler scanning
        /// </summary>
        public static IServiceCollection AddCQRS(
            this IServiceCollection services,
            Func<Type, bool>? typeFilter = null,
            params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
            {
                throw new ArgumentException("At least one assembly must be provided", nameof(assemblies));
            }

            // Register dispatcher
            services.TryAddScoped<ICQRSDispatcher, CQRSDispatcher>();

            // Register CQRS Manager
            services.TryAddScoped<ICQRSManager, CQRSManager>();

            // Auto-register handlers
            RegisterHandlers(services, assemblies, typeFilter);

            return services;
        }

        /// <summary>
        /// Add CQRS with validation pipeline
        /// </summary>
        public static IServiceCollection AddCQRSWithValidation(
            this IServiceCollection services,
            params Assembly[] assemblies)
        {
            services.AddCQRS(assemblies);

            // Register validation pipeline behavior
            services.AddTransient(typeof(ICQRSPipelineBehavior<,>), typeof(CQRSValidationPipeline<,>));

            // Register validators from assemblies
            services.AddValidators(assemblies);

            return services;
        }

        /// <summary>
        /// Add CQRS with all pipelines (validation, logging, performance)
        /// </summary>
        public static IServiceCollection AddCQRSWithPipelines(
            this IServiceCollection services,
            params Assembly[] assemblies)
        {
            services.AddCQRS(assemblies);

            services.AddTransient(typeof(ICQRSPipelineBehavior<,>), typeof(CQRSValidationPipeline<,>));
            services.AddTransient(typeof(ICQRSPipelineBehavior<,>), typeof(CQRSTransactionPipeline<,>));

            services.AddValidators(assemblies);

            return services;
        }

        private static void RegisterHandlers(IServiceCollection services, Assembly[] assemblies, Func<Type, bool>? typeFilter = null)
        {
            // Default filter: include all concrete classes
            Func<Type, bool> filter = typeFilter ?? (t => t.IsClass && !t.IsAbstract);

            foreach (Assembly assembly in assemblies)
            {
                // Register command handlers with result
                var commandHandlersWithResult = assembly.GetTypes()
                    .Where(t => filter(t) && !t.IsInterface)
                    .SelectMany(t => t.GetInterfaces()
                        .Where(i => i.IsGenericType &&
                                   i.GetGenericArguments().Length == 2 &&
                                   i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))
                        .Select(i => new { Interface = i, Implementation = t }))
                    .ToList();

                foreach (var handler in commandHandlersWithResult)
                {
                    services.TryAddScoped(handler.Interface, handler.Implementation);
                }

                // Register command handlers without result
                var commandHandlersWithoutResult = assembly.GetTypes()
                    .Where(t => filter(t) && !t.IsInterface)
                    .SelectMany(t => t.GetInterfaces()
                        .Where(i => i.IsGenericType &&
                                   i.GetGenericArguments().Length == 1 &&
                                   i.GetGenericTypeDefinition() == typeof(ICommandHandler<>))
                        .Select(i => new { Interface = i, Implementation = t }))
                    .ToList();

                foreach (var handler in commandHandlersWithoutResult)
                {
                    services.TryAddScoped(handler.Interface, handler.Implementation);
                }

                // Register query handlers
                var queryHandlers = assembly.GetTypes()
                    .Where(t => filter(t) && !t.IsInterface)
                    .SelectMany(t => t.GetInterfaces()
                        .Where(i => i.IsGenericType &&
                                   i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
                        .Select(i => new { Interface = i, Implementation = t }))
                    .ToList();

                foreach (var handler in queryHandlers)
                {
                    services.TryAddScoped(handler.Interface, handler.Implementation);
                }
            }
        }
    }

}
