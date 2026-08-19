using System.Reflection;
using System.Text.Json;
using TaindSoft.Core.Configuration;

namespace TaindSoft.Core.Settings
{
    /// <summary>
    /// TODO: Document class AbstractSettings
    /// </summary>
    public class AbstractSettings<T> where T : class, ISettings, new()
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public static string BuildKey(PropertyInfo pi)
        {
            SettingKeyAttribute? attr = pi.GetCustomAttribute<SettingKeyAttribute>();
            return attr != null && !string.IsNullOrWhiteSpace(attr.Key) ? attr.Key : $"{typeof(T).Name}:{pi.Name}";
        }

        public static async Task<T> LoadAsync(IConfigProvider provider, string module = "")
        {
            T result = new();
            PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo p in props)
            {
                string key = BuildKey(p);
                string? raw = await provider.GetValueAsync(key, module);
                if (!string.IsNullOrEmpty(raw))
                {
                    try
                    {
                        object? val = JsonSerializer.Deserialize(raw, p.PropertyType, _jsonOptions);
                        p.SetValue(result, val);
                    }
                    catch
                    {
                        // ignore parse errors
                    }
                }
            }
            return result;
        }

        public static async Task SaveAsync(IConfigProvider provider, T settings, string module = "")
        {
            PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo p in props)
            {
                string key = BuildKey(p);
                object? val = p.GetValue(settings);
                string serialized = JsonSerializer.Serialize(val, _jsonOptions);
                await provider.SetValueAsync(key, serialized, module);
            }
        }
    }
}
