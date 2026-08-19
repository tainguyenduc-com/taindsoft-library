namespace TaindSoft.Core.Localization.Abstractions
{
    /// <summary>
    /// Represents a localized string with key and value
    /// </summary>
    public record LocalizedString
    {
        /// <summary>
        /// The resource key
        /// </summary>
        public string Key { get; init; }

        /// <summary>
        /// The localized value
        /// </summary>
        public string Value { get; init; }

        /// <summary>
        /// Indicates if the resource was not found
        /// </summary>
        public bool IsResourceNotFound { get; init; }

        /// <summary>
        /// Initializes a new instance of LocalizedString
        /// </summary>
        public LocalizedString(string key, string value, bool isResourceNotFound = false)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Value = value ?? key; // Fall back to key if value is not found
            IsResourceNotFound = isResourceNotFound;
        }

        /// <summary>
        /// Implicit conversion to string
        /// </summary>
        public static implicit operator string(LocalizedString localizedString) => localizedString.Value;

        /// <summary>
        /// Returns the localized value
        /// </summary>
        public override string ToString()
        {
            return Value;
        }
    }

    /// <summary>
    /// Provides localized strings for a specific culture
    /// </summary>
    public interface IStringLocalizer
    {
        /// <summary>
        /// Gets a localized string by key
        /// </summary>
        /// <param name="key">The resource key</param>
        /// <returns>The localized string</returns>
        LocalizedString this[string key] { get; }

        /// <summary>
        /// Gets a localized string by key with format arguments
        /// </summary>
        /// <param name="key">The resource key</param>
        /// <param name="args">Format arguments</param>
        /// <returns>The formatted localized string</returns>
        LocalizedString this[string key, params object[] args] { get; }

        /// <summary>
        /// Gets all localized strings for the current culture
        /// </summary>
        /// <param name="includeParentCultures">Whether to include parent culture resources</param>
        /// <returns>Collection of localized strings</returns>
        IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures = true);
    }

    /// <summary>
    /// Generic version of IStringLocalizer for type-safe localization
    /// </summary>
    /// <typeparam name="T">The type being localized</typeparam>
    public interface IStringLocalizer<T> : IStringLocalizer
    {
    }
}
