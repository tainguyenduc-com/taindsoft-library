using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace TaindSoft.Core.Application.Validation
{
    /// <summary>
    /// Extension methods for registering validators
    /// </summary>
    public static class ValidationServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all validators from the specified assemblies
        /// </summary>
        public static IServiceCollection AddValidators(
            this IServiceCollection services,
            params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
            {
                throw new ArgumentException("At least one assembly must be provided", nameof(assemblies));
            }

            foreach (Assembly assembly in assemblies)
            {
                List<Type> validatorTypes = assembly.GetTypes()
                    .Where(t => !t.IsAbstract && !t.IsInterface)
                    .Where(t => t.BaseType != null &&
                               t.BaseType.IsGenericType &&
                               t.BaseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
                    .ToList();

                foreach (Type validatorType in validatorTypes)
                {
                    Type baseType = validatorType.BaseType!;
                    Type validatedType = baseType.GetGenericArguments()[0];
                    Type validatorInterfaceType = typeof(IValidator<>).MakeGenericType(validatedType);

                    services.TryAddScoped(validatorInterfaceType, validatorType);
                    services.TryAddScoped(typeof(IValidator), validatorType);
                }
            }

            return services;
        }

        /// <summary>
        /// Registers all validators from the calling assembly
        /// </summary>
        public static IServiceCollection AddValidatorsFromAssembly(
            this IServiceCollection services,
            Assembly assembly)
        {
            return services.AddValidators(assembly);
        }

        /// <summary>
        /// Registers all validators from the assembly containing the specified type
        /// </summary>
        public static IServiceCollection AddValidatorsFromAssemblyContaining<T>(
            this IServiceCollection services)
        {
            return services.AddValidators(typeof(T).Assembly);
        }
    }

}
