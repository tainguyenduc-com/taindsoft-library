namespace TaindSoft.Core.Infrastructure.Seeding
{
    /// <summary>
    /// Represents a single permission entry to seed into the database at startup.
    /// </summary>
    public sealed record PermissionSeedDefinition(
        string Code,
        string Name,
        string Category,
        string Module);

    /// <summary>
    /// Implemented per module to declare permissions that must be seeded
    /// and auto-granted to the Admin role at startup.
    /// </summary>
    public interface IModulePermissionProvider
    {
        IEnumerable<PermissionSeedDefinition> GetPermissions();
    }
}
