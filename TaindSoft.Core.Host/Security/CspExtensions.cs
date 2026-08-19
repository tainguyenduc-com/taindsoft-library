using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TaindSoft.Core.Host.Security;

/// <summary>
/// Options for CSP configuration
/// </summary>
public sealed class CspOptions
{
    internal List<CspPolicyRule> Rules { get; } = new();

    /// <summary>
    /// Add a CSP policy rule
    /// </summary>
    public CspOptions AddRule(Func<PathString, bool> pathMatcher, CspPolicy policy, bool? isDevelopment = null)
    {
        Rules.Add(new CspPolicyRule
        {
            PathMatcher = pathMatcher,
            Policy = policy,
            IsDevelopment = isDevelopment
        });
        return this;
    }

    /// <summary>
    /// Add a CSP policy for a specific path prefix
    /// </summary>
    public CspOptions AddPolicyForPath(string pathPrefix, CspPolicy policy, bool? isDevelopment = null)
    {
        return AddRule(
            path => path.StartsWithSegments(pathPrefix, StringComparison.OrdinalIgnoreCase),
            policy,
            isDevelopment);
    }

    /// <summary>
    /// Add default TaindSoft CSP policies for main host
    /// </summary>
    public CspOptions AddDefaultMainHostPolicies()
    {
        // Auth paths: same for both Dev and Prod
        AddPolicyForPath("/connect", CspDefaults.AuthPolicy);
        AddPolicyForPath("/auth", CspDefaults.AuthPolicy);

        // /admin paths: different for Dev/Prod
        AddPolicyForPath("/admin", CspDefaults.AdminProductionPolicy, isDevelopment: false);
        AddPolicyForPath("/admin", CspDefaults.AdminDevelopmentPolicy, isDevelopment: true);

        // Default paths: different for Dev/Prod
        AddRule(_ => true, CspDefaults.DefaultProductionPolicy, isDevelopment: false);
        AddRule(_ => true, CspDefaults.DefaultDevelopmentPolicy, isDevelopment: true);

        return this;
    }

    /// <summary>
    /// Add default TaindSoft CSP policies for backoffice host
    /// </summary>
    public CspOptions AddDefaultBackofficePolicies()
    {
        // Auth paths: same for both Dev and Prod
        AddPolicyForPath("/connect", CspDefaults.AuthPolicy);
        AddPolicyForPath("/auth", CspDefaults.AuthPolicy);

        // /admin paths: different for Dev/Prod
        AddPolicyForPath("/admin", CspDefaults.AdminProductionPolicy, isDevelopment: false);
        AddPolicyForPath("/admin", CspDefaults.AdminDevelopmentPolicy, isDevelopment: true);

        // Default paths: different for Dev/Prod (no WASM for backoffice)
        AddRule(_ => true, CspDefaults.BackofficeDefaultProductionPolicy, isDevelopment: false);
        AddRule(_ => true, CspDefaults.BackofficeDefaultDevelopmentPolicy, isDevelopment: true);

        return this;
    }
}

/// <summary>
/// Extension methods for CSP registration
/// </summary>
public static class CspExtensions
{
    /// <summary>
    /// Add TaindSoft CSP services
    /// </summary>
    public static IServiceCollection AddTaindSoftCsp(
        this IServiceCollection services,
        Action<CspOptions> configure)
    {
        var options = new CspOptions();
        configure(options);

        // Register provider with rules
        services.AddSingleton<ICspPolicyProvider>(sp =>
        {
            var provider = new CspPolicyProvider();
            foreach (var rule in options.Rules)
            {
                provider.AddRule(rule);
            }
            return provider;
        });

        // Register nonce provider
        services.AddSingleton<INonceProvider, NonceProvider>();

        return services;
    }

    /// <summary>
    /// Use TaindSoft CSP middleware
    /// </summary>
    public static IApplicationBuilder UseTaindSoftCsp(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CspMiddleware>();
    }
}
