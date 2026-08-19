using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using TaindSoft.Core.Configuration;
namespace TaindSoft.Core.Config
{
    /// <summary>
    /// TODO: Document class HttpConfigProvider
    /// </summary>
    public class HttpConfigProvider(HttpClient http, JsonSerializerOptions json, ILogger<HttpConfigProvider> logger) : IConfigProvider
    {
        private readonly HttpClient _http = http;
        private readonly JsonSerializerOptions _json = json;
        private readonly ILogger _logger = logger;

        public async Task<string?> GetValueAsync(string key, string scope = "")
        {
            try
            {
                string url = $"/api/v1/system/config/{Uri.EscapeDataString(key)}";
                using HttpResponseMessage resp = await _http.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return null;

                using var stream = await resp.Content.ReadAsStreamAsync();
                using JsonDocument doc = await JsonDocument.ParseAsync(stream);

                if (doc.RootElement.TryGetProperty("data", out JsonElement data) && data.ValueKind != JsonValueKind.Null)
                {
                    if (data.TryGetProperty("value", out JsonElement val))
                    {
                        return val.GetString();
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HttpConfigProvider: failed to get key {Key}", key);
                return null;
            }
        }

        public async Task SetValueAsync(string key, string value, string scope = "")
        {
            try
            {
                var payload = new
                {
                    Key = key,
                    Scope = scope,
                    Type = "string",
                    Value = value,
                    Version = 1,
                    UpdatedBy = "system",
                    Module = "System",
                    IsEncrypted = false,
                    Description = string.Empty
                };

                HttpResponseMessage resp = await _http.PutAsJsonAsync("/api/v1/system/config", payload, _json);
                resp.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HttpConfigProvider: failed to set key {Key}", key);
                throw;
            }
        }
    }
}
