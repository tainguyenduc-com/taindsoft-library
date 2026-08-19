using Microsoft.AspNetCore.Authentication;

namespace TaindSoft.Core.HttpApi.Security.ExternalApi
{
    /// <summary>
    /// TODO: Document class ExternalApiOptions
    /// </summary>
    public class ExternalApiOptions : AuthenticationSchemeOptions
    {
        public int TimestampWindowSeconds { get; set; } = 120;
        public string SignatureHeader { get; set; } = "X-Signature";
        public string TimestampHeader { get; set; } = "X-Timestamp";
        public string AuthorizationScheme { get; set; } = "ApiKey"; // Authorization: ApiKey <clientId>
    }
}
