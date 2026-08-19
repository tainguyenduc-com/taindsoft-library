using System.Collections.Frozen;

namespace TaindSoft.Core.Host.Security;

/// <summary>
/// Immutable CSP policy containing directive → value mappings
/// </summary>
public sealed record CspPolicy
{
    public FrozenDictionary<string, string> Directives { get; init; } = FrozenDictionary<string, string>.Empty;

    public CspPolicy()
    {
    }

    public CspPolicy(Dictionary<string, string> directives)
    {
        Directives = directives.ToFrozenDictionary();
    }

    /// <summary>
    /// Build the CSP header value from directives, replacing {nonce} placeholder if present
    /// </summary>
    public string BuildHeaderValue(string? nonce = null)
    {
        var parts = new List<string>(Directives.Count);
        foreach (var kvp in Directives)
        {
            var value = kvp.Value;
            if (!string.IsNullOrWhiteSpace(nonce) && value.Contains("{nonce}", StringComparison.Ordinal))
            {
                value = value.Replace("{nonce}", nonce, StringComparison.Ordinal);
            }
            parts.Add($"{kvp.Key} {value}");
        }
        return string.Join("; ", parts) + ";";
    }
}
