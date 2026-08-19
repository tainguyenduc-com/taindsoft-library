namespace TaindSoft.Core.Application.Validation
{
    /// <summary>
    /// Represents the result of a validation
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Gets a value indicating whether validation succeeded
        /// </summary>
        public bool IsValid => !Errors.Any();

        /// <summary>
        /// Gets the collection of validation failures
        /// </summary>
        public IList<ValidationFailure> Errors { get; }

        /// <summary>
        /// Creates a new instance of ValidationResult
        /// </summary>
        public ValidationResult()
        {
            Errors = [];
        }

        /// <summary>
        /// Creates a new instance of ValidationResult with failures
        /// </summary>
        public ValidationResult(IEnumerable<ValidationFailure> failures)
        {
            Errors = [.. failures];
        }

        /// <summary>
        /// Returns a dictionary of errors grouped by property name
        /// </summary>
        public Dictionary<string, string[]> ToDictionary()
        {
            return Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.ErrorMessage).ToArray());
        }

        /// <summary>
        /// Returns a string representation of all errors
        /// </summary>
        public override string ToString()
        {
            return string.Join(Environment.NewLine, Errors.Select(e => e.ErrorMessage));
        }
    }

    /// <summary>
    /// Represents a single validation failure
    /// </summary>
    /// <remarks>
    /// Creates a new instance of ValidationFailure
    /// </remarks>
    public class ValidationFailure(string propertyName, string errorMessage)
    {
        /// <summary>
        /// Gets or sets the property name that failed validation
        /// </summary>
        public string PropertyName { get; set; } = propertyName ?? throw new ArgumentNullException(nameof(propertyName));

        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        public string ErrorMessage { get; set; } = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));

        /// <summary>
        /// Gets or sets the attempted value
        /// </summary>
        public object? AttemptedValue { get; set; }

        /// <summary>
        /// Gets or sets the error code
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Returns a string representation of the validation failure
        /// </summary>
        public override string ToString()
        {
            return ErrorMessage;
        }
    }

}
