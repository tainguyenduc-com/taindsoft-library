using Microsoft.Extensions.DependencyInjection;
using TaindSoft.Core.Identity.CurrentUsers;
using TaindSoft.Core.Identity.PasswordHashers;
using TaindSoft.Core.Identity.PermissionCheckers;
using TaindSoft.Core.PermissionCheckers;

namespace TaindSoft.Core.Identity
{
    /// <summary>
    /// TODO: Document class IdentityCollectionExtensions
    /// </summary>
    public static class IdentityCollectionExtensions
    {
        /// <summary>
        /// Adds permission checking services for modules that need to verify permissions.
        /// Requires UserManagement API to be accessible.
        /// </summary>
        public static IServiceCollection AddIdentityUtilities(
            this IServiceCollection services)
        {
            _ = services.AddScoped<ICurrentUserService, CurrentUserService>();
            _ = services.AddScoped<IPermissionChecker, HttpPermissionChecker>();
            _ = services.AddScoped<IPasswordHasher, PBKDF2PasswordHasher>();

            return services;
        }
    }
}
