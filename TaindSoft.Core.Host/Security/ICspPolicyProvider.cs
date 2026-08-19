using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace TaindSoft.Core.Host.Security;

/// <summary>
/// Resolves CSP policy based on HttpContext and environment
/// </summary>
public interface ICspPolicyProvider
{
    /// <summary>
    /// Get the CSP policy for the current request
    /// </summary>
    CspPolicy GetPolicy(HttpContext context, IWebHostEnvironment environment);
}
