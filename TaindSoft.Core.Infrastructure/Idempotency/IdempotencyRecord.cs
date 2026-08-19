using TaindSoft.Core.Domain.Entities;

namespace TaindSoft.Core.Infrastructure.Idempotency
{
    /// <summary>
    /// Stores idempotent request responses to prevent duplicate processing
    /// </summary>
    /// <summary>
    /// Represents a persisted idempotency key record used to deduplicate operations.
    /// </summary>
    public class IdempotencyRecord : Entity
    {
        /// <summary>
        /// Unique idempotency key provided by client (e.g., UUID)
        /// </summary>
        public string Key { get; private set; } = string.Empty;

        /// <summary>
        /// SHA256 hash of request (method + path + body)
        /// Used to detect request mutations with same key
        /// </summary>
        public string RequestHash { get; private set; } = string.Empty;

        /// <summary>
        /// Serialized response body (JSON or other format)
        /// </summary>
        public string ResponseBody { get; private set; } = string.Empty;

        /// <summary>
        /// HTTP status code of original response
        /// </summary>
        public int StatusCode { get; private set; }

        /// <summary>
        /// When this record was created
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// When this record expires (default 24 hours from creation)
        /// </summary>
        public DateTime ExpiresAt { get; private set; }

        // EF Core constructor
        private IdempotencyRecord() { }

        /// <summary>
        /// Create new idempotency record
        /// </summary>
        public IdempotencyRecord(
            string key,
            string requestHash,
            string responseBody,
            int statusCode,
            DateTime createdAt,
            int expirationHours = 24)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            RequestHash = requestHash ?? throw new ArgumentNullException(nameof(requestHash));
            ResponseBody = responseBody ?? throw new ArgumentNullException(nameof(responseBody));
            StatusCode = statusCode;
            CreatedAt = createdAt;
            ExpiresAt = createdAt.AddHours(expirationHours);
        }

        /// <summary>
        /// Check if this record has expired
        /// </summary>
        public bool IsExpired(DateTime utcNow)
        {
            return utcNow > ExpiresAt;
        }
    }
}
