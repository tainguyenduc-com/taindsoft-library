using Microsoft.AspNetCore.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using TaindSoft.Core.PermissionCheckers;

namespace TaindSoft.Core.Identity.PermissionCheckers
{
    /// <summary>
    /// Permission checker that calls UserManagement API via HTTP.
    /// Used by other modules to verify permissions.
    /// </summary>
    public class HttpPermissionChecker(
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory) : IPermissionChecker
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        private readonly HttpClient _httpClient = httpClientFactory.CreateClient("UserManagement");

        public Guid GetCurrentUserId()
        {
            HttpContext httpContext = _httpContextAccessor.HttpContext
                ?? throw new InvalidArgumentException("HttpContext is not available");

            Claim userIdClaim = httpContext.User.FindFirst("sub")
                ?? httpContext.User.FindFirst("userId")
                ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new NotFoundException("User ID not found in token");

            return !Guid.TryParse(userIdClaim.Value, out Guid userId) ? throw new InvalidDataException("Invalid user ID format") : userId;
        }

        public async Task<bool> HasPermissionAsync(string permissionCode, CancellationToken cancellationToken = default)
        {
            try
            {
                // Short-circuit for Admin role: if current principal has Admin role, grant all permissions
                HttpContext? ctx = _httpContextAccessor.HttpContext;
                if (ctx != null)
                {
                    ClaimsPrincipal user = ctx.User;
                    if (user != null && (user.IsInRole("Admin") || user.HasClaim(c => c.Type == "role" && c.Value == "Admin") || user.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "Admin")))
                    {
                        return true;
                    }
                }

                Guid userId = GetCurrentUserId();

                // Call UserManagement API to check permission
                HttpResponseMessage response = await _httpClient.GetAsync(
                    $"api/v1/identity/permissions/users/{userId}/check?permissionCode={Uri.EscapeDataString(permissionCode)}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                PermissionCheckResponse? result = await response.Content.ReadFromJsonAsync<PermissionCheckResponse>(cancellationToken: cancellationToken);
                return result?.HasPermission ?? false;
            }
            catch (Exception ex)
            {
                throw new InternalServerErrorException(ex.Message);
            }
        }

        public async Task RequirePermissionAsync(string permissionCode, CancellationToken cancellationToken = default)
        {
            bool hasPermission = await HasPermissionAsync(permissionCode, cancellationToken);
            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("Access denied");
            }
        }

        private class PermissionCheckResponse
        {
            public bool HasPermission { get; set; }
        }
    }
}
