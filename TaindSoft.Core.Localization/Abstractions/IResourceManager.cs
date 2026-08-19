namespace TaindSoft.Core.Localization.Abstractions
{
    /// <summary>
    /// Manages loading and caching of localization resources
    /// </summary>
    public interface IResourceManager
    {
        /// <summary>
        /// Gets a localized string from resources
        /// </summary>
        /// <param name="key">The resource key (e.g., "Common:Error")</param>
        /// <param name="culture">The culture (e.g., "en", "vi"). If null, uses current culture</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The localized value, or null if not found</returns>
        Task<string?> GetStringAsync(string key, string? culture = null, CancellationToken ct = default);

        /// <summary>
        /// Gets all localized strings for a culture
        /// </summary>
        /// <param name="culture">The culture code</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Dictionary of all localized strings</returns>
        Task<IDictionary<string, string>> GetAllStringsAsync(string culture, CancellationToken ct = default);

        /// <summary>
        /// Checks if a resource exists
        /// </summary>
        /// <param name="key">The resource key</param>
        /// <param name="culture">The culture code</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if resource exists</returns>
        Task<bool> ResourceExistsAsync(string key, string culture, CancellationToken ct = default);

        /// <summary>
        /// Gets a nested value using dot notation (e.g., "Validation.Required")
        /// </summary>
        /// <param name="key">The resource key with dot notation</param>
        /// <param name="culture">The culture code</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The nested value or null if not found</returns>
        Task<string?> GetNestedStringAsync(string key, string? culture = null, CancellationToken ct = default);
    }
}
