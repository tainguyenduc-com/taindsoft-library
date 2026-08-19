namespace TaindSoft.Core.HttpApi.Endpoints
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    /// <summary>
    /// TODO: Document class EndpointOpenApiDescriptionAttribute
    /// </summary>
    public sealed class EndpointOpenApiDescriptionAttribute(string description) : Attribute
    {
        public string Description { get; } = description;
    }
}
