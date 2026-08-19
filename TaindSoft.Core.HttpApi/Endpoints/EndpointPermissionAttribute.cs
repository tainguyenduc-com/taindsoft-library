namespace TaindSoft.Core.HttpApi.Endpoints
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    /// <summary>
    /// TODO: Document class EndpointPermissionAttribute
    /// </summary>
    public sealed class EndpointPermissionAttribute(string permissionCode, bool enable = false) : Attribute
    {
        public string PermissionCode { get; } = permissionCode;
        public bool Enable { get; } = enable;
    }
}
