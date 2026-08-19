using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;

namespace TaindSoft.Core.Host.Security;

/// <summary>
/// Default nonce provider using RandomNumberGenerator
/// </summary>
public sealed class NonceProvider : INonceProvider
{
    private const string NonceKey = "CSPNonce";

    public string GenerateNonce(HttpContext context)
    {
        var bytes = new byte[16]; // 128-bit
        RandomNumberGenerator.Fill(bytes);
        var nonce = Convert.ToBase64String(bytes);
        context.Items[NonceKey] = nonce;
        return nonce;
    }

    public string? GetNonce(HttpContext context)
    {
        return context.Items[NonceKey] as string;
    }
}
