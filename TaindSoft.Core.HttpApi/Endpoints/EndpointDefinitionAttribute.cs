namespace TaindSoft.Core.HttpApi.Endpoints
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    /// <summary>
    /// TODO: Document class EndpointDefinitionAttribute
    /// </summary>
    public sealed class EndpointDefinitionAttribute(string method, string route, string name, string tag) : Attribute
    {
        public string Method { get; } = method;
        public string Route { get; } = route;
        public string Name { get; } = name;
        public string Tag { get; } = tag;
    }
}
