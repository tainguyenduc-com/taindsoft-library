namespace TaindSoft.Core.Application.CQRS
{
    /// <summary>
    /// Base class for CQRS result objects
    /// Provides common properties for result tracking
    /// </summary>
    public abstract record CQRSResult
    {
        /// <summary>
        /// Indicates whether the operation was successful
        /// </summary>
        public bool IsSuccess { get; init; } = true;

        /// <summary>
        /// Error message if operation failed
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Validation errors (if any)
        /// </summary>
        public Dictionary<string, string[]>? ValidationErrors { get; init; }

        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static TResult Success<TResult>() where TResult : CQRSResult, new()
        {
            return new TResult { IsSuccess = true };
        }

        /// <summary>
        /// Creates a failed result with error message
        /// </summary>
        public static TResult Failure<TResult>(string errorMessage) where TResult : CQRSResult, new()
        {
            return new TResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }

        /// <summary>
        /// Creates a failed result with validation errors
        /// </summary>
        public static TResult ValidationFailure<TResult>(Dictionary<string, string[]> validationErrors)
            where TResult : CQRSResult, new()
        {
            return new TResult
            {
                IsSuccess = false,
                ValidationErrors = validationErrors
            };
        }
    }

    /// <summary>
    /// Generic CQRS result with data payload
    /// </summary>
    /// <typeparam name="TData">Type of data returned</typeparam>
    public record CQRSResult<TData> : CQRSResult
    {
        /// <summary>
        /// The data payload
        /// </summary>
        public TData? Data { get; init; }

        /// <summary>
        /// Creates a successful result with data
        /// </summary>
        public static CQRSResult<TData> Success(TData data)
        {
            return new CQRSResult<TData>
            {
                IsSuccess = true,
                Data = data
            };
        }
    }

}
