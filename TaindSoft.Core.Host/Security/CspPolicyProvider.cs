using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace TaindSoft.Core.Host.Security;

/// <summary>
/// Default CSP policy provider that matches path patterns and environment
/// </summary>
public sealed class CspPolicyProvider : ICspPolicyProvider
{
    private readonly List<CspPolicyRule> _rules = new();

    public void AddRule(CspPolicyRule rule)
    {
        _rules.Add(rule);
    }

    public CspPolicy GetPolicy(HttpContext context, IWebHostEnvironment environment)
    {
        var path = context.Request.Path;
        var isDevelopment = environment.IsDevelopment();

        // Find first matching rule
        foreach (var rule in _rules)
        {
            if (rule.Matches(path, isDevelopment))
            {
                return rule.Policy;
            }
        }

        // Fallback to empty policy if no match
        return new CspPolicy();
    }
}

/// <summary>
/// A CSP policy rule: path pattern + environment filter + policy
/// </summary>
public sealed class CspPolicyRule
{
    public Func<PathString, bool> PathMatcher { get; init; } = _ => false;
    public bool? IsDevelopment { get; init; } // null = both
    public CspPolicy Policy { get; init; } = new();

    public bool Matches(PathString path, bool isDevelopment)
    {
        if (IsDevelopment.HasValue && IsDevelopment.Value != isDevelopment)
        {
            return false;
        }
        return PathMatcher(path);
    }
}
