namespace TaindSoft.AdminUI.Services
{
    /// <summary>
    /// Core model for a system configuration item, returned by <see cref="ISystemConfigProvider"/>.
    /// </summary>
    public class ConfigProviderItem
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Module { get; set; }
        public string? Scope { get; set; }
        public bool IsEncrypted { get; set; }
    }

    /// <summary>
    /// Provider interface for system configuration/settings access.
    /// Define in <c>TaindSoft.AdminUI</c>; implement in <c>SystemManagement.AdminUI</c>;
    /// inject via DI wherever modules need config values without a direct cross-module reference.
    /// </summary>
    public interface ISystemConfigProvider
    {
        Task<IEnumerable<ConfigProviderItem>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<ConfigProviderItem?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    }
}
