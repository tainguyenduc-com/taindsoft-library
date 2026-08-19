namespace TaindSoft.Core.HttpApi.Endpoints
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    /// <summary>
    /// TODO: Document class EndpointMetadataAttribute
    /// </summary>
    public sealed class EndpointMetadataAttribute(string name, string tag) : Attribute
    {
        public string Name { get; } = name;
        public string Tag { get; } = tag;
    }
}
