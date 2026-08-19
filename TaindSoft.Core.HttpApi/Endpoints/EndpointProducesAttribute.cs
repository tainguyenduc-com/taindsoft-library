namespace TaindSoft.Core.HttpApi.Endpoints
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    /// <summary>
    /// TODO: Document class EndpointProducesAttribute
    /// </summary>
    public sealed class EndpointProducesAttribute(int statusCode, Type? responseType = null) : Attribute
    {
        public int StatusCode { get; } = statusCode;
        public Type? ResponseType { get; } = responseType;
    }
}
