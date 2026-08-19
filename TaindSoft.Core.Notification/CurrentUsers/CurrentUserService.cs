using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace TaindSoft.Core.Identity.CurrentUsers
{
    /// <summary>
    /// TODO: Document class CurrentUserService
    /// </summary>
    public class CurrentUserService(IHttpContextAccessor httpContextAccessor, ILogger<CurrentUserService> logger) : ICurrentUserService
    {
        public Guid GetCurrentUserId()
        {
            ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;

            // Debug logging
            logger.LogDebug("User.Identity.IsAuthenticated: {Auth}", user?.Identity?.IsAuthenticated);
            logger.LogDebug("Claims count: {Count}", user?.Claims?.Count());

            if (user?.Claims != null)
            {
                foreach (Claim claim in user.Claims)
                {
                    logger.LogDebug("Claim: {Type} = {Value}", claim.Type, claim.Value);
                }
            }

            // Try multiple claim types for backward compatibility
            string? userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user?.FindFirst("sub")?.Value
                ?? user?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            logger.LogDebug("userIdClaim value: {UserIdClaim}", userIdClaim);

            if (Guid.TryParse(userIdClaim, out Guid userId))
            {
                logger.LogDebug("Successfully parsed userId: {UserId}", userId);
                return userId;
            }

            logger.LogWarning("Failed to parse userId from claim: {UserIdClaim}", userIdClaim);
            throw new UnauthorizedException("User is not authenticated.");
        }

        public Task RequirePermissionAsync(string permissionCode, CancellationToken cancellationToken = default)
        {
            // Here you would typically check the user's permissions.
            // For simplicity, we'll assume all authenticated users have all permissions.
            Guid userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                throw new UnauthorizedException("User does not have the required permission.");
            }
            return Task.CompletedTask;
        }
    }
}
