namespace TaindSoft.Core.PermissionCheckers
{
    /// <summary>
    /// Interface for checking user permissions.
    /// Each module should implement this to call UserManagement's permission service.
    /// </summary>
    public interface IPermissionChecker
    {
        /// <summary>
        /// Checks if current user has the specified permission.
        /// </summary>
        Task<bool> HasPermissionAsync(string permissionCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Requires the current user to have the specified permission.
        /// Throws UnauthorizedAccessException if user doesn't have permission.
        /// </summary>
        Task RequirePermissionAsync(string permissionCode, CancellationToken cancellationToken = default);
    }
}
