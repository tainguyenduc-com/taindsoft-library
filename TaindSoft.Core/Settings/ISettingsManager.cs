namespace TaindSoft.Core.Settings
{
    /// <summary>
    /// TODO: Document interface ISettingsManager
    /// </summary>
    public interface ISettingsManager
    {
        Task<T> GetAsync<T>(string module = "") where T : class, ISettings, new();
        Task SaveAsync<T>(T settings, string module = "") where T : class, ISettings, new();
    }
}
