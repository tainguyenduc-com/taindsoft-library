namespace TaindSoft.AdminUI.Services
{
    /// <summary>
    /// Core model representing a user, returned by <see cref="IUserProvider"/>.
    /// </summary>
    public class UserProviderItem
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public bool IsActive { get; set; }
        public List<RoleProviderItem> Roles { get; set; } = [];
    }

    /// <summary>
    /// Core model representing a role, returned by <see cref="IUserProvider"/>.
    /// </summary>
    public class RoleProviderItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystem { get; set; }
    }

    /// <summary>
    /// Provider interface for user and role lookup operations.
    /// Define in <c>TaindSoft.AdminUI</c>; implement in <c>IdentityService.AdminUI</c>;
    /// inject via DI wherever modules need user/role data without a direct cross-module reference.
    /// </summary>
    public interface IUserProvider
    {
        Task<(List<UserProviderItem> Items, int Total)> SearchAsync(
            string? query,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default);

        Task<UserProviderItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<List<RoleProviderItem>> GetRolesAsync(CancellationToken cancellationToken = default);
    }
}
