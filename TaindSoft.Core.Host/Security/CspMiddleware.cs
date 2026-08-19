using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace TaindSoft.Core.Host.Security;

/// <summary>
/// Middleware that generates nonce and sets Content-Security-Policy header
/// </summary>
public sealed class CspMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICspPolicyProvider _policyProvider;
    private readonly INonceProvider _nonceProvider;
    private readonly IWebHostEnvironment _environment;

    public CspMiddleware(
        RequestDelegate next,
        ICspPolicyProvider policyProvider,
        INonceProvider nonceProvider,
        IWebHostEnvironment environment)
    {
        _next = next;
        _policyProvider = policyProvider;
        _nonceProvider = nonceProvider;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate nonce before calling next middleware
        var nonce = _nonceProvider.GenerateNonce(context);

        await _next(context);

        // Set CSP header if response hasn't started
        if (!context.Response.HasStarted)
        {
            var policy = _policyProvider.GetPolicy(context, _environment);
            if (policy.Directives.Count > 0)
            {
                var headerValue = policy.BuildHeaderValue(nonce);
                context.Response.Headers.ContentSecurityPolicy = headerValue;
            }
        }
    }
}
