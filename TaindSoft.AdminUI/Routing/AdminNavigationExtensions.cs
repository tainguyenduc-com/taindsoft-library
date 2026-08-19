using Microsoft.AspNetCore.Components;

namespace TaindSoft.AdminUI.Routing
{
    /// <summary>
    /// Extension methods for NavigationManager that transform admin navigation URLs
    /// to use the configured admin prefix (/admin/... → /{prefix}/...).
    /// Must be initialized once at startup via <see cref="Initialize"/>.
    /// </summary>
    public static class AdminNavigationExtensions
    {
        private static string _prefix = "admin";

        /// <summary>
        /// Initialize the admin prefix used by Go() to transform outgoing URLs.
        /// Call once at application startup after reading configuration.
        /// </summary>
        public static void Initialize(string prefix)
        {
            _prefix = prefix.Trim('/');
        }

        /// <summary>
        /// Navigate to an admin path, automatically transforming /admin/... to
        /// /{configuredPrefix}/... so all admin navigation uses the configured URL prefix.
        /// </summary>
        /// <param name="navigation">The NavigationManager instance.</param>
        /// <param name="adminPath">
        /// Path starting with /admin/... (e.g., "/admin/dashboard").
        /// Non-admin paths and paths already using the configured prefix pass through unchanged.
        /// </param>
        /// <param name="forceLoad">
        /// If true, bypasses client-side routing and forces the browser to load
        /// the new page (server-side navigation).
        /// </param>
        public static void Go(this NavigationManager navigation, string adminPath, bool forceLoad = false)
        {
            if (navigation is null)
            {
                throw new ArgumentNullException(nameof(navigation));
            }

            var transformed = TransformOutgoing(adminPath);
            navigation.NavigateTo(transformed, forceLoad);
        }

        /// <summary>
        /// Transforms /admin/... to /{prefix}/... for outgoing navigation.
        /// </summary>
        private static string TransformOutgoing(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            // If prefix is default "admin", no transformation needed
            if (_prefix == "admin")
            {
                return path;
            }

            // Normalize: ensure leading slash for comparison
            var normalizedPath = path.StartsWith('/') ? path : "/" + path;

            // /admin/something → /{prefix}/something
            if (normalizedPath.StartsWith("/admin/", StringComparison.OrdinalIgnoreCase))
            {
                return "/" + _prefix + normalizedPath[6..];
            }

            // /admin → /{prefix}
            if (string.Equals(normalizedPath, "/admin", StringComparison.OrdinalIgnoreCase))
            {
                return "/" + _prefix;
            }

            // Already using the configured prefix or a public page — pass through
            var prefixSegment = "/" + _prefix;
            if (normalizedPath.StartsWith(prefixSegment + "/", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalizedPath, prefixSegment, StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            return path;
        }
    }
}
