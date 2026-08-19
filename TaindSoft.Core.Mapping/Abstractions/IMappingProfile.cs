namespace TaindSoft.Core.Mapping.Abstractions
{
    /// <summary>
    /// Marker interface for mapping profiles
    /// </summary>
    public interface IMappingProfile
    {
        /// <summary>
        /// Configures the mappings for this profile
        /// </summary>
        /// <param name="config">The mapping configuration</param>
        void Configure(IMappingConfiguration config);
    }
}
