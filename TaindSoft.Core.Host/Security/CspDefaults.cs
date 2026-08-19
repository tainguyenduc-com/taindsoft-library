namespace TaindSoft.Core.Host.Security;

/// <summary>
/// Default CSP policies for TaindSoft hosts
/// </summary>
public static class CspDefaults
{
    /// <summary>
    /// Strict CSP for /connect and /auth paths (both Dev and Prod)
    /// </summary>
    public static CspPolicy AuthPolicy => new(new Dictionary<string, string>
    {
        [CspDirectives.DefaultSrc] = "'none'",
        [CspDirectives.ScriptSrc] = "'self' 'nonce-{nonce}'",
        [CspDirectives.StyleSrc] = "'self' 'nonce-{nonce}'",
        [CspDirectives.ImgSrc] = "'self' data:",
        [CspDirectives.FontSrc] = "'none'",
        [CspDirectives.ConnectSrc] = "'self'",
        [CspDirectives.FrameAncestors] = "'none'",
        [CspDirectives.BaseUri] = "'self'",
        [CspDirectives.FormAction] = "'self'",
        [CspDirectives.ObjectSrc] = "'none'"
    });

    /// <summary>
    /// CSP for /admin path in Production (no unsafe-inline/unsafe-eval)
    /// </summary>
    public static CspPolicy AdminProductionPolicy => new(new Dictionary<string, string>
    {
        [CspDirectives.DefaultSrc] = "'self'",
        [CspDirectives.ImgSrc] = "'self' data: https:",
        [CspDirectives.ScriptSrc] = "'self' 'nonce-{nonce}' https://cdn.jsdelivr.net https://cdn.quilljs.com",
        [CspDirectives.StyleSrc] = "'self' 'nonce-{nonce}' https://fonts.googleapis.com https://cdn.jsdelivr.net https://cdn.quilljs.com",
        [CspDirectives.FontSrc] = "'self' https://fonts.gstatic.com https://cdn.jsdelivr.net data:",
        [CspDirectives.ConnectSrc] = "'self' ws: wss:"
    });

    /// <summary>
    /// CSP for /admin path in Development (with unsafe-inline/unsafe-eval for BrowserLink)
    /// </summary>
    public static CspPolicy AdminDevelopmentPolicy => new(new Dictionary<string, string>
    {
        [CspDirectives.DefaultSrc] = "'self'",
        [CspDirectives.ImgSrc] = "'self' data: https:",
        [CspDirectives.ScriptSrc] = "'self' 'nonce-{nonce}' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdn.quilljs.com",
        [CspDirectives.StyleSrc] = "'self' 'nonce-{nonce}' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net https://cdn.quilljs.com",
        [CspDirectives.FontSrc] = "'self' https://fonts.gstatic.com https://cdn.jsdelivr.net data:",
        [CspDirectives.ConnectSrc] = "'self' http://localhost:* ws: wss:"
    });

    /// <summary>
    /// CSP for default paths (non-admin, non-auth) in Production
    /// </summary>
    public static CspPolicy DefaultProductionPolicy => new(new Dictionary<string, string>
    {
        [CspDirectives.DefaultSrc] = "'self'",
        [CspDirectives.ImgSrc] = "'self' data: https:",
        [CspDirectives.ScriptSrc] = "'self' 'nonce-{nonce}' 'wasm-unsafe-eval' https://cdn.jsdelivr.net",
        [CspDirectives.StyleSrc] = "'self' 'nonce-{nonce}' https://fonts.googleapis.com https://cdn.jsdelivr.net",
        [CspDirectives.FontSrc] = "'self' https://fonts.gstatic.com",
        [CspDirectives.ConnectSrc] = "'self' ws: wss:",
        [CspDirectives.FrameAncestors] = "'none'",
        [CspDirectives.ObjectSrc] = "'none'",
        [CspDirectives.BaseUri] = "'self'"
    });

    /// <summary>
    /// CSP for default paths in Development (with unsafe-inline/unsafe-eval)
    /// </summary>
    public static CspPolicy DefaultDevelopmentPolicy => new(new Dictionary<string, string>
    {
        [CspDirectives.DefaultSrc] = "'self'",
        [CspDirectives.ImgSrc] = "'self' data: https:",
        [CspDirectives.ScriptSrc] = "'self' 'nonce-{nonce}' 'unsafe-inline' 'unsafe-eval' 'wasm-unsafe-eval' https://cdn.jsdelivr.net",
        [CspDirectives.StyleSrc] = "'self' 'nonce-{nonce}' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net",
        [CspDirectives.FontSrc] = "'self' https://fonts.gstatic.com",
        [CspDirectives.ConnectSrc] = "'self' http://localhost:* ws: wss:",
        [CspDirectives.FrameAncestors] = "'none'",
        [CspDirectives.ObjectSrc] = "'none'",
        [CspDirectives.BaseUri] = "'self'"
    });

    /// <summary>
    /// CSP for backoffice default paths in Production (no WASM)
    /// </summary>
    public static CspPolicy BackofficeDefaultProductionPolicy => new(new Dictionary<string, string>
    {
        [CspDirectives.DefaultSrc] = "'self'",
        [CspDirectives.ImgSrc] = "'self' data: https:",
        [CspDirectives.ScriptSrc] = "'self' 'nonce-{nonce}' https://cdn.jsdelivr.net",
        [CspDirectives.StyleSrc] = "'self' 'nonce-{nonce}' https://fonts.googleapis.com https://cdn.jsdelivr.net",
        [CspDirectives.FontSrc] = "'self' https://fonts.gstatic.com",
        [CspDirectives.ConnectSrc] = "'self' ws: wss:"
    });

    /// <summary>
    /// CSP for backoffice default paths in Development
    /// </summary>
    public static CspPolicy BackofficeDefaultDevelopmentPolicy => new(new Dictionary<string, string>
    {
        [CspDirectives.DefaultSrc] = "'self'",
        [CspDirectives.ImgSrc] = "'self' data: https:",
        [CspDirectives.ScriptSrc] = "'self' 'nonce-{nonce}' 'unsafe-inline' https://cdn.jsdelivr.net",
        [CspDirectives.StyleSrc] = "'self' 'nonce-{nonce}' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net",
        [CspDirectives.FontSrc] = "'self' https://fonts.gstatic.com",
        [CspDirectives.ConnectSrc] = "'self' http://localhost:* ws: wss:"
    });
}
