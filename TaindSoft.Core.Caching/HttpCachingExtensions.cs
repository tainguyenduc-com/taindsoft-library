using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;

namespace TaindSoft.Core.Caching
{
    /// <summary>
    /// HTTP caching and ETag support for API responses
    /// </summary>
    public static class HttpCachingExtensions
    {
        /// <summary>
        /// Middleware to add HTTP caching headers and ETag support for GET requests
        /// Applies: Cache-Control, ETag, Last-Modified
        /// </summary>
        public static IApplicationBuilder UseHttpCaching(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                HttpResponse response = context.Response;
                Stream originalBodyStream = response.Body;

                // Only apply caching to GET requests
                if (string.Equals(context.Request.Method, HttpMethods.Get, StringComparison.OrdinalIgnoreCase))
                {
                    using MemoryStream memoryStream = new();
                    response.Body = memoryStream;

                    await next();

                    // Generate ETag from response content
                    if (response.StatusCode == StatusCodes.Status200OK)
                    {
                        byte[] content = memoryStream.ToArray();
                        string etag = GenerateETag(content);

                        // Set cache headers for GET requests
                        response.Headers["Cache-Control"] = "public, max-age=300"; // 5 minutes default
                        response.Headers["ETag"] = etag;
                        response.Headers["Last-Modified"] = DateTime.UtcNow.ToString("R");

                        // Check If-None-Match header for conditional requests
                        if (context.Request.Headers.TryGetValue("If-None-Match", out Microsoft.Extensions.Primitives.StringValues clientETag) &&
                            clientETag == etag)
                        {
                            response.StatusCode = StatusCodes.Status304NotModified;
                            response.ContentLength = 0;
                            await response.Body.FlushAsync();
                            return;
                        }
                    }

                    // Copy content to original stream
                    await memoryStream.CopyToAsync(originalBodyStream);
                }
                else
                {
                    response.Body = originalBodyStream;
                    await next();

                    // Set no-cache for POST/PUT/DELETE/PATCH
                    response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                    response.Headers["Pragma"] = "no-cache";
                    response.Headers["Expires"] = "0";
                }
            });
        }

        /// <summary>
        /// Generate ETag from content using SHA256
        /// </summary>
        private static string GenerateETag(byte[] content)
        {
            byte[] hash = SHA256.HashData(content);
            return $"\"{Convert.ToBase64String(hash)}\"";
        }

        /// <summary>
        /// Set cache control header for specific responses
        /// Usage in endpoint: response.Headers.CacheControl = "public, max-age=3600"
        /// </summary>
        public static void SetPublicCacheControl(this HttpResponse response, int maxAgeSeconds = 300)
        {
            response.Headers["Cache-Control"] = $"public, max-age={maxAgeSeconds}";
        }

        /// <summary>
        /// Set private cache control (only cacheable by the client, not by proxies)
        /// </summary>
        public static void SetPrivateCacheControl(this HttpResponse response, int maxAgeSeconds = 300)
        {
            response.Headers["Cache-Control"] = $"private, max-age={maxAgeSeconds}";
        }

        /// <summary>
        /// Disable caching for sensitive responses
        /// </summary>
        public static void DisableCaching(this HttpResponse response)
        {
            response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            response.Headers["Pragma"] = "no-cache";
            response.Headers["Expires"] = "0";
        }
    }
}
