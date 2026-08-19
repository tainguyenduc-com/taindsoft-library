using Microsoft.AspNetCore.Components;
using System.Globalization;
using TaindSoft.PublicUI.Services;

namespace TaindSoft.PublicUI.Pages
{
    /// <summary>
    /// Base class for Razor pages that need locale loading and simple localization helpers.
    /// Keeps responsibility small: ensure locale is loaded and provide `Localize` helpers.
    /// </summary>
    public abstract class AbstractPage : ComponentBase
    {
        [Inject]
        protected ILocaleHelper Locale { get; set; } = default!;

        protected static string CurrentLocale => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        protected override async Task OnInitializedAsync()
        {
            await EnsureLocaleLoadedAsync();
            await base.OnInitializedAsync();
        }

        protected virtual async Task EnsureLocaleLoadedAsync()
        {
            try
            {
                await Locale.EnsureLoadedAsync(CurrentLocale);
            }
            catch
            {
                // swallow: don't block rendering on locale fetch failures
            }
        }

        protected string Localize(string key)
        {
            return Locale.Localize(CurrentLocale, key) ?? key;
        }

        protected string Localize(string locale, string key)
        {
            return Locale.Localize(locale, key) ?? key;
        }
    }
}
