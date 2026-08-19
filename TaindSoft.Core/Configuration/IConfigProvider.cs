namespace TaindSoft.Core.Configuration
{
    /// <summary>
    /// TODO: Document interface IConfigProvider
    /// </summary>
    public interface IConfigProvider
    {
        Task<string?> GetValueAsync(string key, string scope = "");
        Task SetValueAsync(string key, string value, string scope = "");
    }
}
