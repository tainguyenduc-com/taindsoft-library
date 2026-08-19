using Microsoft.AspNetCore.Components;

namespace TaindSoft.AdminUI.Navigation
{
    /// <summary>
    /// TODO: Document class AdminMenuRouteUtility
    /// </summary>
    internal static class AdminMenuRouteUtility
    {
        public static bool IsRouteActive(NavigationManager navigationManager, string? targetUrl)
        {
            string currentPath = NormalizePath(new Uri(navigationManager.Uri).AbsolutePath);
            string targetPath = NormalizePath(ResolveMenuUrl(navigationManager, targetUrl));

            if (string.IsNullOrEmpty(targetPath))
            {
                return string.IsNullOrEmpty(currentPath);
            }

            return currentPath.Equals(targetPath, StringComparison.OrdinalIgnoreCase)
                || currentPath.StartsWith(targetPath + "/", StringComparison.OrdinalIgnoreCase);
        }

        public static string ResolveMenuUrl(NavigationManager navigationManager, string? targetUrl)
        {
            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                return navigationManager.BaseUri;
            }

            string normalized = targetUrl.Trim();
            if (Uri.TryCreate(normalized, UriKind.Absolute, out Uri? absolute))
            {
                return absolute.PathAndQuery + absolute.Fragment;
            }

            string relativePath = normalized.StartsWith('/') ? normalized[1..] : normalized;
            Uri resolved = new(new Uri(navigationManager.BaseUri), relativePath);
            return resolved.PathAndQuery + resolved.Fragment;
        }

        public static string NormalizePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string normalized = path.Trim();

            if (Uri.TryCreate(normalized, UriKind.Absolute, out Uri? absolute))
            {
                normalized = absolute.AbsolutePath;
            }
            else
            {
                normalized = StripQueryAndFragment(normalized);
            }

            return normalized.Trim('/');
        }

        private static string StripQueryAndFragment(string path)
        {
            int queryIndex = path.IndexOf('?');
            int fragmentIndex = path.IndexOf('#');

            if (queryIndex < 0 && fragmentIndex < 0)
            {
                return path;
            }

            if (queryIndex < 0)
            {
                return path[..fragmentIndex];
            }

            if (fragmentIndex < 0)
            {
                return path[..queryIndex];
            }

            int cutIndex = Math.Min(queryIndex, fragmentIndex);
            return path[..cutIndex];
        }
    }
}
