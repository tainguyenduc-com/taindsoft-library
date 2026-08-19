using Microsoft.AspNetCore.Http;

namespace TaindSoft.Core.Host.Security;

/// <summary>
/// Generates and stores CSP nonces per request
/// </summary>
public interface INonceProvider
{
    /// <summary>
    /// Generate a new base64 nonce (128-bit) and store in HttpContext.Items["CSPNonce"]
    /// </summary>
    string GenerateNonce(HttpContext context);

    /// <summary>
    /// Retrieve the nonce from HttpContext.Items["CSPNonce"]
    /// </summary>
    string? GetNonce(HttpContext context);
}
