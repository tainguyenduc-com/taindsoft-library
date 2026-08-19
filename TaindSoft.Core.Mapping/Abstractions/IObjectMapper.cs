namespace TaindSoft.Core.Mapping.Abstractions
{
    /// <summary>
    /// Represents an object mapper that converts between different types
    /// </summary>
    public interface IObjectMapper
    {
        /// <summary>
        /// Maps an object to a destination type
        /// </summary>
        /// <typeparam name="TDestination">The destination type</typeparam>
        /// <param name="source">The source object to map</param>
        /// <returns>The mapped destination object</returns>
        TDestination Map<TDestination>(object source) where TDestination : class;

        /// <summary>
        /// Maps an object of one type to another type
        /// </summary>
        /// <typeparam name="TSource">The source type</typeparam>
        /// <typeparam name="TDestination">The destination type</typeparam>
        /// <param name="source">The source object</param>
        /// <returns>The mapped destination object</returns>
        TDestination Map<TSource, TDestination>(TSource source)
            where TSource : class
            where TDestination : class;

        /// <summary>
        /// Maps a source object to an existing destination object
        /// </summary>
        /// <typeparam name="TSource">The source type</typeparam>
        /// <typeparam name="TDestination">The destination type</typeparam>
        /// <param name="source">The source object</param>
        /// <param name="destination">The destination object to map into</param>
        void Map<TSource, TDestination>(TSource source, TDestination destination)
            where TSource : class
            where TDestination : class;
    }
}
