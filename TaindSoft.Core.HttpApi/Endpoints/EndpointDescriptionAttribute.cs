namespace TaindSoft.Core.HttpApi.Endpoints
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    /// <summary>
    /// TODO: Document class EndpointDescriptionAttribute
    /// </summary>
    public sealed class EndpointDescriptionAttribute(string description) : Attribute
    {
        public string Description { get; } = description;
    }
}
