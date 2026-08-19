using System.Linq.Expressions;

namespace TaindSoft.Core.Mapping.Abstractions
{
    /// <summary>
    /// Represents the configuration for mappings between types
    /// </summary>
    public interface IMappingConfiguration
    {
        /// <summary>
        /// Creates a mapping from source type to destination type
        /// </summary>
        /// <typeparam name="TSource">The source type</typeparam>
        /// <typeparam name="TDestination">The destination type</typeparam>
        /// <returns>A mapping builder for configuring the mapping</returns>
        IMappingBuilder<TSource, TDestination> CreateMap<TSource, TDestination>()
            where TSource : class
            where TDestination : class;
    }

    /// <summary>
    /// Represents a builder for configuring a mapping between two types
    /// </summary>
    /// <typeparam name="TSource">The source type</typeparam>
    /// <typeparam name="TDestination">The destination type</typeparam>
    public interface IMappingBuilder<TSource, TDestination>
        where TSource : class
        where TDestination : class
    {
        /// <summary>
        /// Configures how a destination property should be mapped
        /// </summary>
        /// <typeparam name="TProperty">The property type</typeparam>
        /// <param name="destinationSelector">The destination property selector</param>
        /// <param name="sourceSelector">The source property selector</param>
        /// <returns>The mapping builder for fluent configuration</returns>
        IMappingBuilder<TSource, TDestination> ForMember<TProperty>(
            Expression<Func<TDestination, TProperty>> destinationSelector,
            Expression<Func<TSource, TProperty>> sourceSelector);

        /// <summary>
        /// Configures how a destination property should be mapped using a resolver function
        /// </summary>
        /// <typeparam name="TProperty">The property type</typeparam>
        /// <param name="destinationSelector">The destination property selector</param>
        /// <param name="resolver">A function to resolve the destination value from source</param>
        /// <returns>The mapping builder for fluent configuration</returns>
        IMappingBuilder<TSource, TDestination> ForMember<TProperty>(
            Expression<Func<TDestination, TProperty>> destinationSelector,
            Func<TSource, TProperty> resolver);

        /// <summary>
        /// Ignores mapping for a destination property
        /// </summary>
        /// <typeparam name="TProperty">The property type</typeparam>
        /// <param name="destinationSelector">The destination property selector</param>
        /// <returns>The mapping builder for fluent configuration</returns>
        IMappingBuilder<TSource, TDestination> Ignore<TProperty>(
            Expression<Func<TDestination, TProperty>> destinationSelector);
    }
}
