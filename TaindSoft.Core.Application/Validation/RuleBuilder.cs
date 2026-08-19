namespace TaindSoft.Core.Application.Validation
{
    /// <summary>
    /// Rule builder interface for fluent API
    /// </summary>
    public interface IRuleBuilder<T, TProperty>
    {
        /// <summary>
        /// Adds a validator to the rule
        /// </summary>
        IRuleBuilder<T, TProperty> SetValidator(PropertyValidatorBase<T, TProperty> validator);

        /// <summary>
        /// Specifies a custom error message
        /// </summary>
        IRuleBuilder<T, TProperty> WithMessage(string errorMessage);
    }

    /// <summary>
    /// Concrete rule builder implementation
    /// </summary>
    internal class RuleBuilder<T, TProperty>(ValidationRule<T, TProperty> rule) : IRuleBuilder<T, TProperty>
    {
        private readonly ValidationRule<T, TProperty> _rule = rule;
        private string? _customMessage;

        public IRuleBuilder<T, TProperty> SetValidator(PropertyValidatorBase<T, TProperty> validator)
        {
            if (_customMessage != null)
            {
                validator.SetCustomMessage(_customMessage);
                _customMessage = null;
            }

            _rule.AddValidator(validator);
            return this;
        }

        public IRuleBuilder<T, TProperty> WithMessage(string errorMessage)
        {
            _customMessage = errorMessage;
            return this;
        }
    }

    /// <summary>
    /// Base class for property validators
    /// </summary>
    public abstract class PropertyValidatorBase<T, TProperty> : IPropertyValidator<T, TProperty>
    {
        protected string? CustomMessage { get; private set; }

        public abstract bool IsValid(T instance, TProperty value);

        protected abstract string DefaultErrorMessage { get; }

        public string GetErrorMessage(string propertyName)
        {
            string message = CustomMessage ?? DefaultErrorMessage;
            return message.Replace("{PropertyName}", propertyName);
        }

        public void SetCustomMessage(string message)
        {
            CustomMessage = message;
        }
    }

}
