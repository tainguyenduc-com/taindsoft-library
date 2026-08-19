namespace TaindSoft.Core.Localization.Abstractions
{
    /// <summary>
    /// Provides and manages the current culture
    /// </summary>
    public interface ICultureProvider
    {
        /// <summary>
        /// Gets the current culture code (e.g., "en", "vi")
        /// </summary>
        /// <returns>Culture code</returns>
        string GetCurrentCulture();

        /// <summary>
        /// Sets the current culture
        /// </summary>
        /// <param name="culture">Culture code</param>
        void SetCulture(string culture);

        /// <summary>
        /// Gets all supported cultures
        /// </summary>
        /// <returns>Collection of supported culture codes</returns>
        IEnumerable<string> GetSupportedCultures();

        /// <summary>
        /// Checks if a culture is supported
        /// </summary>
        /// <param name="culture">Culture code to check</param>
        /// <returns>True if culture is supported</returns>
        bool IsSupported(string culture);

        /// <summary>
        /// Gets the fallback culture (e.g., "en" for "en-US")
        /// </summary>
        /// <param name="culture">Culture code</param>
        /// <returns>Fallback culture code</returns>
        string GetFallbackCulture(string culture);
    }
}
