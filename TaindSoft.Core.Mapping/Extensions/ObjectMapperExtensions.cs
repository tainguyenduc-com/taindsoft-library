using TaindSoft.Core.Mapping.Abstractions;

namespace TaindSoft.Core.Mapping.Extensions
{
    /// <summary>
    /// Extension methods for IObjectMapper to support collection mapping
    /// </summary>
    public static class ObjectMapperExtensions
    {
        /// <summary>
        /// Maps a collection of objects to a list of destination type
        /// </summary>
        public static List<TDestination> MapList<TSource, TDestination>(
            this IObjectMapper mapper,
            IEnumerable<TSource> source)
            where TSource : class
            where TDestination : class
        {
            if (source == null)
            {
                return [];
            }

            return [.. source.Select(item => mapper.Map<TSource, TDestination>(item))];
        }

        /// <summary>
        /// Maps a collection of objects to a list of destination type (non-generic source)
        /// </summary>
        public static List<TDestination> MapList<TDestination>(
            this IObjectMapper mapper,
            IEnumerable<object> source)
            where TDestination : class
        {
            if (source == null)
            {
                return [];
            }

            return [.. source.Select(item => mapper.Map<TDestination>(item))];
        }
    }
}
