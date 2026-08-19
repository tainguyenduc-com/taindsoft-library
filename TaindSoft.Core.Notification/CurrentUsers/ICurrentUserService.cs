namespace TaindSoft.Core.Identity.CurrentUsers
{
    /// <summary>
    /// TODO: Document interface ICurrentUserService
    /// </summary>
    public interface ICurrentUserService
    {
        Guid GetCurrentUserId();

        Task RequirePermissionAsync(string permissionCode, CancellationToken cancellationToken = default);
    }
}
