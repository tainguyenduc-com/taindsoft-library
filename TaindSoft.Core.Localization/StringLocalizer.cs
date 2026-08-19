using System.Globalization;
using TaindSoft.Core.Localization.Abstractions;

namespace TaindSoft.Core.Localization
{
    /// <summary>
    /// Default implementation of string localizer
    /// </summary>
    public sealed class StringLocalizer(IResourceManager resourceManager, ICultureProvider cultureProvider) : IStringLocalizer
    {
        private readonly IResourceManager _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
        private readonly ICultureProvider _cultureProvider = cultureProvider ?? throw new ArgumentNullException(nameof(cultureProvider));

        public LocalizedString this[string key]
        {
            get
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentNullException(nameof(key));
                }

                string culture = _cultureProvider.GetCurrentCulture();
                string? value = _resourceManager.GetStringAsync(key, culture).Result;

                return new LocalizedString(key, value ?? key, value == null);
            }
        }

        public LocalizedString this[string key, params object[] args]
        {
            get
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentNullException(nameof(key));
                }

                string culture = _cultureProvider.GetCurrentCulture();
                string? value = _resourceManager.GetStringAsync(key, culture).Result;

                if (value == null)
                {
                    return new LocalizedString(key, key, true);
                }

                try
                {
                    string formatted = string.Format(CultureInfo.CurrentCulture, value, args);
                    return new LocalizedString(key, formatted, false);
                }
                catch
                {
                    // If formatting fails, return original value
                    return new LocalizedString(key, value, false);
                }
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures = true)
        {
            string culture = _cultureProvider.GetCurrentCulture();
            IDictionary<string, string> strings = _resourceManager.GetAllStringsAsync(culture).Result;

            List<LocalizedString> result = strings.Select(kvp => new LocalizedString(kvp.Key, kvp.Value)).ToList();

            if (includeParentCultures && culture.Contains('-'))
            {
                string parentCulture = culture.Split('-')[0];
                IDictionary<string, string> parentStrings = _resourceManager.GetAllStringsAsync(parentCulture).Result;

                foreach (KeyValuePair<string, string> kvp in parentStrings.Where(p => !strings.ContainsKey(p.Key)))
                {
                    result.Add(new LocalizedString(kvp.Key, kvp.Value));
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Generic version of string localizer
    /// </summary>
    public sealed class StringLocalizer<T>(IStringLocalizer innerLocalizer) : IStringLocalizer<T>
    {
        private readonly IStringLocalizer _innerLocalizer = innerLocalizer ?? throw new ArgumentNullException(nameof(innerLocalizer));

        public LocalizedString this[string key] => _innerLocalizer[key];

        public LocalizedString this[string key, params object[] args] => _innerLocalizer[key, args];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures = true)
        {
            return _innerLocalizer.GetAllStrings(includeParentCultures);
        }
    }
}
