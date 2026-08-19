using System.Text.Json.Serialization;

namespace TaindSoft.Core.Dtos
{
    public record ApiResponse
    {
        /// <summary>
        /// Indicates whether the request was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Top-level machine-readable response code (e.g. HTTP-like or module code)
        /// Recommended: string following conventions (e.g. "200" for success, or module code)
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Response message (success or error)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Message { get; set; }

        /// <summary>
        /// Error details (if applicable)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ErrorDetails? Error { get; set; }

        public static ApiResponse Successful()
        {
            return new ApiResponse
            {
                Success = true,
            };
        }

        /// <summary>
        /// Create a failure response
        /// </summary>
        public static ApiResponse Failure(string message, ErrorDetails? error = null, string? code = null)
        {
            return new ApiResponse
            {
                Success = false,
                Code = code ?? error?.Code ?? ErrorCodes.Unknown,
                Message = message,
                Error = error
            };
        }
    }

    /// <summary>
    /// Standard API response envelope for all API endpoints
    /// </summary>
    public record ApiResponse<T> : ApiResponse
    {
        /// <summary>
        /// Response data
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public T? Data { get; set; }


        /// <summary>
        /// Create a successful response
        /// </summary>
        public static ApiResponse<T> Successful(T? data)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data
            };
        }
    }

    /// <summary>
    /// Error details in API response
    /// </summary>
    public record ErrorDetails
    {
        /// <summary>
        /// Error code (use values from ErrorCodes or module-specific codes)
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Error description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Validation errors by field (for validation failures)
        /// </summary>
        public Dictionary<string, string[]>? ValidationErrors { get; set; }

        /// <summary>
        /// Stack trace (only in development)
        /// </summary>
        public string? StackTrace { get; set; }
    }
}
