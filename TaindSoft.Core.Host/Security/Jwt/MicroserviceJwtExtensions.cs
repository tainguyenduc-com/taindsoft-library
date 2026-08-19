using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TaindSoft.Core.Host.Security.Jwt
{
    /// <summary>
    /// JWT authentication extensions for microservice deployment (FUTURE).
    /// Reserved for asymmetric signing (RS256/ES256), JWKS, key rotation.
    /// NOT IMPLEMENTED - Use AddMonolithJwt() for current deployment.
    /// </summary>
    public static class MicroserviceJwtExtensions
    {
        /// <summary>
        /// Configures JWT authentication for microservice deployment topology.
        /// FUTURE: Will support RS256/ES256, JWKS endpoint, multiple signing keys, key rotation.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Application configuration</param>
        /// <returns>Service collection for chaining</returns>
        /// <exception cref="NotImplementedException">Always throws - not yet implemented</exception>
        public static IServiceCollection AddMicroserviceJwt(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            throw new NotImplementedException(
                "Microservice JWT strategy is not implemented yet. " +
                "This extension is reserved for future distributed deployment scenarios " +
                "with asymmetric signing (RS256/ES256), JWKS endpoints, and key rotation support. " +
                "Current deployment: Use AddMonolithJwt() for symmetric HS256 signing.");
        }
    }
}
