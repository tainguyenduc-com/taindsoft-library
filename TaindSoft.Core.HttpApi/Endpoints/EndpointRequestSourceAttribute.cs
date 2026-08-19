namespace TaindSoft.Core.HttpApi.Endpoints
{
    /// <summary>
    /// TODO: Document enum EndpointRequestSource
    /// </summary>
    public enum EndpointRequestSource
    {
        Body,
        Query,
        HttpRequest
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    /// <summary>
    /// TODO: Document class EndpointRequestSourceAttribute
    /// </summary>
    public sealed class EndpointRequestSourceAttribute(EndpointRequestSource source) : Attribute
    {
        public EndpointRequestSource Source { get; } = source;
    }
}
