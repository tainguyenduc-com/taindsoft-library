namespace TaindSoft.Core.Application.Validation
{
    /// <summary>
    /// Defines a validator for a particular type
    /// </summary>
    public interface IValidator<in T>
    {
        /// <summary>
        /// Validates the specified instance
        /// </summary>
        /// <param name="instance">The instance to validate</param>
        /// <returns>A ValidationResult object containing any validation failures</returns>
        ValidationResult Validate(T instance);

        /// <summary>
        /// Validates the specified instance asynchronously
        /// </summary>
        /// <param name="instance">The instance to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A ValidationResult object containing any validation failures</returns>
        Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Non-generic validator interface
    /// </summary>
    public interface IValidator
    {
        /// <summary>
        /// Validates the specified instance
        /// </summary>
        ValidationResult Validate(object instance);

        /// <summary>
        /// Validates the specified instance asynchronously
        /// </summary>
        Task<ValidationResult> ValidateAsync(object instance, CancellationToken cancellationToken = default);
    }

}
