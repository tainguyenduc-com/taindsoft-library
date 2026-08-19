using Microsoft.AspNetCore.Http;
using System.Globalization;
using TaindSoft.Core.Localization.Abstractions;

namespace TaindSoft.Core.Localization
{
    /// <summary>
    /// Default implementation of culture provider
    /// Supports HTTP context, thread culture, and explicit setting
    /// </summary>
    public sealed class CultureProvider : ICultureProvider
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly IEnumerable<string> _supportedCultures;
        private readonly string _defaultCulture;
        private string? _explicitCulture;

        public CultureProvider(
            IHttpContextAccessor? httpContextAccessor,
            IEnumerable<string> supportedCultures,
            string defaultCulture = "en")
        {
            _httpContextAccessor = httpContextAccessor;
            _supportedCultures = supportedCultures ?? ["en"];
            _defaultCulture = defaultCulture;

            // Validate default culture is in supported list
            if (!_supportedCultures.Contains(_defaultCulture))
            {
                throw new ArgumentException($"Default culture '{_defaultCulture}' must be in supported cultures list");
            }
        }

        public string GetCurrentCulture()
        {
            // 1. Check explicit setting
            if (!string.IsNullOrEmpty(_explicitCulture))
            {
                return _explicitCulture;
            }

            // 2. Check HTTP context for culture claim
            if (_httpContextAccessor?.HttpContext != null)
            {
                string? cultureClaim = _httpContextAccessor.HttpContext.User
                    .FindFirst("culture")?.Value;

                if (!string.IsNullOrEmpty(cultureClaim) && IsSupported(cultureClaim))
                {
                    return cultureClaim;
                }

                // 3. Check Accept-Language header
                string? acceptLanguage = _httpContextAccessor.HttpContext.Request.Headers.AcceptLanguage.FirstOrDefault();
                if (!string.IsNullOrEmpty(acceptLanguage))
                {
                    string? culture = NegotiateCulture(acceptLanguage);
                    if (!string.IsNullOrEmpty(culture))
                    {
                        return culture;
                    }
                }
            }

            // 4. Fall back to current thread culture
            string threadCulture = CultureInfo.CurrentCulture.Name;
            if (IsSupported(threadCulture))
            {
                return threadCulture;
            }

            // 5. Fall back to default
            return _defaultCulture;
        }

        public void SetCulture(string culture)
        {
            if (string.IsNullOrEmpty(culture))
            {
                throw new ArgumentNullException(nameof(culture));
            }

            if (!IsSupported(culture))
            {
                throw new ArgumentException($"Culture '{culture}' is not supported");
            }

            _explicitCulture = culture;

            // Also set thread culture
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo(culture);
            }
            catch
            {
                // Ignore if culture cannot be set on current thread
            }
        }

        public IEnumerable<string> GetSupportedCultures()
        {
            return _supportedCultures;
        }

        public bool IsSupported(string culture)
        {
            if (string.IsNullOrEmpty(culture))
            {
                return false;
            }

            // Check exact match
            if (_supportedCultures.Contains(culture))
            {
                return true;
            }

            // Check base language (e.g., "en" for "en-US")
            string baseLanguage = culture.Split('-')[0];
            return _supportedCultures.Contains(baseLanguage);
        }

        public string GetFallbackCulture(string culture)
        {
            if (string.IsNullOrEmpty(culture))
            {
                return _defaultCulture;
            }

            // If exact match exists, return it
            if (_supportedCultures.Contains(culture))
            {
                return culture;
            }

            // Try base language
            string baseLanguage = culture.Split('-')[0];
            if (_supportedCultures.Contains(baseLanguage))
            {
                return baseLanguage;
            }

            // Return default
            return _defaultCulture;
        }

        /// <summary>
        /// Negotiates culture from Accept-Language header
        /// Example: "en-US,en;q=0.9,vi;q=0.8" ? returns "en"
        /// </summary>
        private string? NegotiateCulture(string acceptLanguage)
        {
            List<string> languages = acceptLanguage
                .Split(',')
                .Select(x => x.Split(';')[0].Trim())
                .ToList();

            foreach (string? lang in languages)
            {
                if (IsSupported(lang))
                {
                    return GetFallbackCulture(lang);
                }
            }

            return null;
        }
    }
}
