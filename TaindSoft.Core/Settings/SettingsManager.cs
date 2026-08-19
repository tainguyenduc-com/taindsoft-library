using TaindSoft.Core.Configuration;

namespace TaindSoft.Core.Settings
{
    /// <summary>
    /// Resolves application settings from configured providers and exposes typed access.
    /// </summary>
    public class SettingsManager(IConfigProvider provider) : ISettingsManager
    {
        private readonly IConfigProvider _provider = provider;

        public async Task<T> GetAsync<T>(string module = "") where T : class, ISettings, new()
        {
            return await AbstractSettings<T>.LoadAsync(_provider, module);
        }

        public async Task SaveAsync<T>(T settings, string module = "") where T : class, ISettings, new()
        {
            await AbstractSettings<T>.SaveAsync(_provider, settings, module);
        }
    }
}
