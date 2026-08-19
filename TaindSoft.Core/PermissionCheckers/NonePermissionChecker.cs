namespace TaindSoft.Core.PermissionCheckers
{
    /// <summary>
    /// Interface for checking user permissions.
    /// Each module should implement this to call UserManagement's permission service.
    /// </summary>
    public class NonePermissionChecker : IPermissionChecker
    {
        public static Guid GetCurrentUserId()
        {
            return Guid.NewGuid();
        }

        public Task<bool> HasPermissionAsync(string permissionCode, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task RequirePermissionAsync(string permissionCode, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
