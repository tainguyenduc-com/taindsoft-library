namespace TaindSoft.Core.Identity.Configuration
{
    /// <summary>
    /// JWT configuration options - Single Source of Truth.
    /// Used for both token generation and validation across all applications.
    /// Bound from "JWT" configuration section.
    /// </summary>
    public class JwtOptions
    {
        /// <summary>
        /// HMAC shared secret for HS256 signing (monolith strategy).
        /// Minimum 256 bits (32 bytes) required.
        /// Generate using: openssl rand -hex 32
        /// </summary>
        public string? HmacSecret { get; set; }

        /// <summary>
        /// Path to the X509 signing certificate file for RS256 signing.
        /// </summary>
        public string? SigningCertificatePath { get; set; }

        /// <summary>
        /// Password for the X509 signing certificate.
        /// </summary>
        public string? SigningCertificatePassword { get; set; }

        /// <summary>
        /// Key ID for signature validation matching.
        /// Optional for monolith, may be required for microservice strategy.
        /// </summary>
        public string? KeyId { get; set; }


        /// <summary>
        /// JWT issuer (iss claim).
        /// Must match between token generation and validation.
        /// </summary>
        public string? Issuer { get; set; }

        /// <summary>
        /// JWT audience (aud claim).
        /// Must match between token generation and validation.
        /// </summary>
        public string? Audiences { get; set; }

        /// <summary>
        /// Token expiration time in seconds.
        /// Default: 3600 seconds (1 hour).
        /// </summary>
        public int ExpiresInSeconds { get; set; } = 3600;

        /// <summary>
        /// Refresh token expiration in seconds.
        /// Default: 604800 seconds (7 days).
        /// </summary>
        public int RefreshExpiresInSeconds { get; set; } = 604800;

        /// <summary>
        /// Whether refresh tokens are enabled.
        /// Default: true.
        /// </summary>
        public bool EnableRefreshTokens { get; set; } = true;

        /// <summary>
        /// Default OAuth scopes to include in tokens.
        /// Default: "openid profile email".
        /// </summary>
        public string? DefaultScopes { get; set; }

        /// <summary>
        /// Previous HMAC secrets kept for grace period during key rotation.
        /// Tokens signed with a previous secret are still validated until they expire.
        /// Each entry should be a valid HMAC secret (min 32 bytes).
        /// </summary>
        public string[]? PreviousHmacSecrets { get; set; }
    }
}
