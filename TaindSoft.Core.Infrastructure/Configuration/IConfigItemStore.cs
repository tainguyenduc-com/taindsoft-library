namespace TaindSoft.Core.Infrastructure.Configuration
{
    /// <summary>
    /// TODO: Document class ConfigEntry
    /// </summary>
    public class ConfigEntry
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsEncrypted { get; set; }
    }

    /// <summary>
    /// TODO: Document interface IConfigItemStore
    /// </summary>
    public interface IConfigItemStore
    {
        Task<IEnumerable<ConfigEntry>> GetAllAsync();
    }
}
