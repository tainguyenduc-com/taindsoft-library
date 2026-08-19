namespace TaindSoft.Core.Identity.PermissionCheckers
{
    /// <summary>
    /// TODO: Document class AuthServerSettings
    /// </summary>
    public class AuthServerSettings
    {
        public string? Authority { get; set; }
        public string? Audience { get; set; }
        public bool RequireHttpsMetadata { get; set; }
    }
}
