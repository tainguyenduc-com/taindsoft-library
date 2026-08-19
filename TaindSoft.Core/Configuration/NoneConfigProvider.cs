namespace TaindSoft.Core.Configuration
{
    /// <summary>
    /// TODO: Document class NoneConfigProvider
    /// </summary>
    public class NoneConfigProvider : IConfigProvider
    {
        public Task<string?> GetValueAsync(string key, string scope = "")
        {
            throw new NotImplementedException();
        }

        public Task SetValueAsync(string key, string value, string scope = "")
        {
            throw new NotImplementedException();
        }
    }
}
