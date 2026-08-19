using Microsoft.Extensions.Configuration;

namespace TaindSoft.Core.Config
{
    /// <summary>
    /// TODO: Document class ConfigurationValidation
    /// </summary>
    public static class ConfigurationValidation
    {
        /// <summary>
        /// Ensure required configuration keys are present. Throws InvalidOperationException
        /// when running outside Development environment and any required key is missing or empty.
        /// Usage: call at startup (Program.cs) after building IConfiguration.
        /// </summary>
        public static void EnsureRequired(this IConfiguration configuration, params string[] requiredKeys)
        {
            if (requiredKeys == null || requiredKeys.Length == 0) return;

            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            var missing = requiredKeys.Where(k => string.IsNullOrWhiteSpace(configuration[k])).ToArray();
            if (missing.Length == 0) return;

            if (!string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Missing required configuration keys: {string.Join(',', missing)}");
            }
        }
    }
}
