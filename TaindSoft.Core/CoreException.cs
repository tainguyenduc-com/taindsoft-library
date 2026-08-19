using System.Net;

namespace TaindSoft.Core
{
    public abstract class CoreException : Exception
    {
        public CoreException(string message, string code) : base(message)
        {
            Code = code;
        }
        public CoreException(string message, Exception inner, string code) : base(message, inner)
        {
            Code = code;
        }

        public string Code { get; init; } = string.Empty;
        public HttpStatusCode HttpStatus { get; init; } = HttpStatusCode.InternalServerError;
    }

    /// <summary>
    /// Represents a validation failure within domain or application logic.
    /// </summary>
    public class ValidationException : CoreException
    {
        public ValidationException(Dictionary<string, string[]> errors)
            : this(errors, "One or more validation errors occurred.", ErrorCodes.ValidationFailed)
        {

        }
        public ValidationException(Dictionary<string, string[]> errors, string message)
            : this(errors, message, ErrorCodes.ValidationFailed)
        {

        }
        public ValidationException(Dictionary<string, string[]> errors, string message, string code) : base(message, code)
        {
            HttpStatus = HttpStatusCode.BadRequest;
            Errors = errors ?? new Dictionary<string, string[]>();
        }
        public Dictionary<string, string[]> Errors { get; init; } = new Dictionary<string, string[]>();

        /// <summary>
        /// Gets localized error messages using IStringLocalizer
        /// </summary>
        /// <remarks>
        /// This method can be called in exception handling middleware to localize error messages.
        /// 
        /// Example:
        /// <code>
        /// try
        /// {
        ///     // Command/Query execution
        /// }
        /// catch (ValidationException ex)
        /// {
        ///     var localizer = sp.GetRequiredService&lt;IStringLocalizer&gt;();
        ///     var localizedErrors = ex.GetLocalizedErrors(localizer);
        ///     // Return localized errors in response
        /// }
        /// </code>
        /// </remarks>
        /// <param name="localizer">The string localizer instance</param>
        /// <returns>Dictionary of localized error messages</returns>
        public Dictionary<string, string[]> GetLocalizedErrors(dynamic localizer)
        {
            if (localizer == null)
            {
                return Errors; // Return original if no localizer provided
            }

            try
            {
                var localizedErrors = new Dictionary<string, string[]>();

                foreach (var kvp in Errors)
                {
                    var localizedMessages = kvp.Value.Select(msg => (string)localizer[msg].Value).ToArray();
                    localizedErrors[kvp.Key] = localizedMessages;
                }

                return localizedErrors;
            }
            catch
            {
                // If localization fails, return original errors
                return Errors;
            }
        }
    }

    /// <summary>
    /// Thrown when an entity or resource cannot be found.
    /// </summary>
    public class NotFoundException : CoreException
    {
        public NotFoundException(string message) : base(message, ErrorCodes.NotFound)
        {
            HttpStatus = HttpStatusCode.NotFound;
        }
    }

    /// <summary>
    /// Thrown when an operation is not authorized.
    /// </summary>
    public class UnauthorizedException : CoreException
    {
        public UnauthorizedException(string message) : base(message, ErrorCodes.Unauthorized)
        {
            HttpStatus = HttpStatusCode.Unauthorized;
        }
    }

    /// <summary>
    /// Represents an invalid operation within application flow.
    /// </summary>
    public class InvalidOperationExceptionEx : CoreException
    {
        public InvalidOperationExceptionEx(string message) : base(message, ErrorCodes.InvalidOperation)
        {
            HttpStatus = HttpStatusCode.BadRequest;
        }
    }

    /// <summary>
    /// Thrown when an argument to a method is invalid.
    /// </summary>
    public class InvalidArgumentException : CoreException
    {
        public InvalidArgumentException(string message) : base(message, ErrorCodes.InvalidArgument)
        {
            HttpStatus = HttpStatusCode.BadRequest;
        }
    }

    /// <summary>
    /// Represents an unexpected server-side error.
    /// </summary>
    public class InternalServerErrorException : CoreException
    {
        public InternalServerErrorException(string message) : base(message, ErrorCodes.InternalServerError)
        {
            HttpStatus = HttpStatusCode.InternalServerError;
        }
    }

    /// <summary>
    /// Specific exception used when a domain entity is not found.
    /// </summary>
    public class EntityNotFoundException : CoreException
    {
        public EntityNotFoundException(string entity, object key) : base($"{entity} with key '{key}' was not found.", ErrorCodes.NotFound)
        {
            HttpStatus = HttpStatusCode.NotFound;
        }
    }

    /// <summary>
    /// Indicates a domain business rule violation.
    /// </summary>
    public class BusinessRuleViolatedException : CoreException
    {
        public BusinessRuleViolatedException(string message) : base(message, ErrorCodes.BusinessRuleViolation)
        {
            HttpStatus = HttpStatusCode.Conflict;
        }
    }
}
