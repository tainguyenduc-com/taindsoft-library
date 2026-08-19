using System.Linq.Expressions;
using TaindSoft.Core.Mapping.Abstractions;

namespace TaindSoft.Core.Mapping
{
    /// <summary>
    /// Base class for defining object mapping profiles
    /// </summary>
    public abstract class MappingProfile : IMappingProfile
    {
        /// <summary>
        /// Configures the mappings for this profile
        /// </summary>
        /// <remarks>
        /// Override this method to define custom mappings using fluent API.
        /// Example:
        /// <code>
        /// public override void Configure(IMappingConfiguration config)
        /// {
        ///     config.CreateMap&lt;User, UserDto&gt;()
        ///         .ForMember(d => d.FullName, s => $"{s.FirstName} {s.LastName}");
        /// }
        /// </code>
        /// </remarks>
        /// <param name="config">The mapping configuration builder</param>
        public abstract void Configure(IMappingConfiguration config);
    }

    /// <summary>
    /// Implementation of IMappingConfiguration for fluent mapping configuration
    /// </summary>
    internal sealed class MappingConfiguration : IMappingConfiguration
    {
        public IMappingBuilder<TSource, TDestination> CreateMap<TSource, TDestination>()
            where TSource : class
            where TDestination : class
        {
            return new MappingBuilder<TSource, TDestination>();
        }
    }

    /// <summary>
    /// Builder for configuring mappings between two types
    /// </summary>
    internal sealed class MappingBuilder<TSource, TDestination> : IMappingBuilder<TSource, TDestination>
        where TSource : class
        where TDestination : class
    {
        public IMappingBuilder<TSource, TDestination> ForMember<TProperty>(
            Expression<Func<TDestination, TProperty>> destinationSelector,
            Expression<Func<TSource, TProperty>> sourceSelector)
        {
            // Custom mapping logic could be stored here for advanced scenarios
            // For now, this is a placeholder for fluent configuration
            return this;
        }

        public IMappingBuilder<TSource, TDestination> ForMember<TProperty>(
            Expression<Func<TDestination, TProperty>> destinationSelector,
            Func<TSource, TProperty> resolver)
        {
            // Custom resolver could be stored and used during mapping
            return this;
        }

        public IMappingBuilder<TSource, TDestination> Ignore<TProperty>(
            Expression<Func<TDestination, TProperty>> destinationSelector)
        {
            // Ignored properties could be stored and skipped during mapping
            return this;
        }
    }
}
