namespace TaindSoft.PublicUI.Services
{
    /// <summary>
    /// TODO: Document interface ILocaleHelper
    /// </summary>
    public interface ILocaleHelper
    {
        Task EnsureLoadedAsync(string locale, CancellationToken cancellationToken = default);
        Task RefreshAsync(CancellationToken cancellationToken = default);
        string? Localize(string locale, string key);
        string? Localize(string key);
    }
}
