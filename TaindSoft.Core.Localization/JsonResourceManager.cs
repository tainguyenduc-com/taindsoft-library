using System.Collections.Concurrent;
using System.Text.Json;
using TaindSoft.Core.Localization.Abstractions;

namespace TaindSoft.Core.Localization
{
    /// <summary>
    /// Resource manager that loads localization resources from JSON files
    /// </summary>
    public sealed class JsonResourceManager(string resourcesPath = "Resources") : IResourceManager
    {
        private readonly ConcurrentDictionary<string, Dictionary<string, object>> _resources = new();
        private readonly string _resourcesPath = resourcesPath ?? throw new ArgumentNullException(nameof(resourcesPath));

        public async Task<string?> GetStringAsync(string key, string? culture = null, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            culture ??= "en";

            try
            {
                Dictionary<string, object> resources = await EnsureResourcesLoadedAsync(culture, ct);

                if (resources.TryGetValue(key, out object? value))
                {
                    return ConvertToString(value);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<IDictionary<string, string>> GetAllStringsAsync(string culture, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(culture))
            {
                throw new ArgumentNullException(nameof(culture));
            }

            try
            {
                Dictionary<string, object> resources = await EnsureResourcesLoadedAsync(culture, ct);

                Dictionary<string, string> result = new();
                foreach (KeyValuePair<string, object> kvp in resources)
                {
                    result[kvp.Key] = ConvertToString(kvp.Value);
                }

                return result;
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        public async Task<bool> ResourceExistsAsync(string key, string culture, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (string.IsNullOrEmpty(culture))
            {
                throw new ArgumentNullException(nameof(culture));
            }

            try
            {
                Dictionary<string, object> resources = await EnsureResourcesLoadedAsync(culture, ct);
                return resources.ContainsKey(key);
            }
            catch
            {
                return false;
            }
        }

        public async Task<string?> GetNestedStringAsync(string key, string? culture = null, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            culture ??= "en";

            try
            {
                Dictionary<string, object> resources = await EnsureResourcesLoadedAsync(culture, ct);
                string[] parts = key.Split(':');

                object? current = resources;

                foreach (string part in parts)
                {
                    if (current is Dictionary<string, object> dict)
                    {
                        if (dict.TryGetValue(part, out object? value))
                        {
                            current = value;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else if (current is JsonElement element)
                    {
                        if (element.TryGetProperty(part, out JsonElement prop))
                        {
                            current = prop;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }

                return ConvertToString(current);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Loads resource file from path
        /// </summary>
        public async Task LoadResourcesAsync(string culture, string filePath, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(culture))
            {
                throw new ArgumentNullException(nameof(culture));
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Resource file not found: {filePath}");
            }

            try
            {
                string json = await File.ReadAllTextAsync(filePath, ct);
                JsonDocument doc = JsonDocument.Parse(json);
                Dictionary<string, object> dict = new();

                FlattenJsonElement(doc.RootElement, dict, "");

                _resources.AddOrUpdate(culture, dict, (k, v) => dict);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load resource file '{filePath}'", ex);
            }
        }

        private async Task<Dictionary<string, object>> EnsureResourcesLoadedAsync(string culture, CancellationToken ct)
        {
            if (_resources.TryGetValue(culture, out Dictionary<string, object>? resources))
            {
                return resources;
            }

            // Try to auto-load from default location
            string filePath = Path.Combine(_resourcesPath, $"{culture}.json");

            if (File.Exists(filePath))
            {
                await LoadResourcesAsync(culture, filePath, ct);
                return _resources.GetOrAdd(culture, []);
            }

            return [];
        }

        /// <summary>
        /// Flattens nested JSON structure into a flat dictionary
        /// </summary>
        private static void FlattenJsonElement(JsonElement element, Dictionary<string, object> dict, string prefix)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty prop in element.EnumerateObject())
                {
                    string key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}:{prop.Name}";

                    if (prop.Value.ValueKind == JsonValueKind.Object)
                    {
                        FlattenJsonElement(prop.Value, dict, key);
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        dict[key] = prop.Value;
                    }
                    else
                    {
                        dict[key] = prop.Value.GetRawText();
                    }
                }
            }
        }

        /// <summary>
        /// Converts JSON value to string
        /// </summary>
        private static string ConvertToString(object? value)
        {
            return value switch
            {
                null => string.Empty,
                string s => s,
                JsonElement je => je.GetRawText().Trim('"'),
                _ => value.ToString() ?? string.Empty
            };
        }
    }
}
