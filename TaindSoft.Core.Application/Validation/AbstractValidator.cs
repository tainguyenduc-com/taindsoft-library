using System.Linq.Expressions;

namespace TaindSoft.Core.Application.Validation
{
    /// <summary>
    /// Base class for validators using fluent interface
    /// </summary>
    public abstract class AbstractValidator<T> : IValidator<T>, IValidator
    {
        private readonly List<IValidationRule<T>> _rules = [];

        /// <summary>
        /// Defines a validation rule for a property
        /// </summary>
        protected IRuleBuilder<T, TProperty> RuleFor<TProperty>(
            Expression<Func<T, TProperty>> expression)
        {
            string propertyName = GetPropertyName(expression);
            Func<T, TProperty> compiledExpression = expression.Compile();

            ValidationRule<T, TProperty> rule = new(propertyName, compiledExpression);
            _rules.Add(rule);

            return new RuleBuilder<T, TProperty>(rule);
        }

        /// <summary>
        /// Validates the instance
        /// </summary>
        public ValidationResult Validate(T instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            List<ValidationFailure> failures = [];

            foreach (IValidationRule<T> rule in _rules)
            {
                IEnumerable<ValidationFailure> ruleFailures = rule.Validate(instance);
                failures.AddRange(ruleFailures);
            }

            return new ValidationResult(failures);
        }

        /// <summary>
        /// Validates the instance asynchronously
        /// </summary>
        public virtual Task<ValidationResult> ValidateAsync(
            T instance,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Validate(instance));
        }

        /// <summary>
        /// Non-generic validate
        /// </summary>
        ValidationResult IValidator.Validate(object instance)
        {
            ArgumentNullException.ThrowIfNull(instance);

            return instance is not T typedInstance
                ? throw new ArgumentException($"Instance must be of type {typeof(T).Name}")
                : Validate(typedInstance);
        }

        /// <summary>
        /// Non-generic validate async
        /// </summary>
        Task<ValidationResult> IValidator.ValidateAsync(
            object instance,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(instance);

            return instance is not T typedInstance
                ? throw new ArgumentException($"Instance must be of type {typeof(T).Name}")
                : ValidateAsync(typedInstance, cancellationToken);
        }

        /// <summary>
        /// Extracts property name from expression
        /// </summary>
        private static string GetPropertyName<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            if (expression.Body is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }

            if (expression.Body is UnaryExpression unaryExpression &&
                unaryExpression.Operand is MemberExpression operand)
            {
                return operand.Member.Name;
            }

            throw new ArgumentException("Expression must be a property accessor", nameof(expression));
        }
    }

    /// <summary>
    /// Validation rule for a property
    /// </summary>
    internal interface IValidationRule<in T>
    {
        IEnumerable<ValidationFailure> Validate(T instance);
    }

    /// <summary>
    /// Concrete validation rule implementation
    /// </summary>
    internal class ValidationRule<T, TProperty>(string propertyName, Func<T, TProperty> propertyFunc) : IValidationRule<T>
    {
        private readonly string _propertyName = propertyName;
        private readonly Func<T, TProperty> _propertyFunc = propertyFunc;
        private readonly List<IPropertyValidator<T, TProperty>> _validators = [];

        public void AddValidator(IPropertyValidator<T, TProperty> validator)
        {
            _validators.Add(validator);
        }

        public IEnumerable<ValidationFailure> Validate(T instance)
        {
            TProperty? propertyValue = _propertyFunc(instance);
            List<ValidationFailure> failures = [];

            foreach (IPropertyValidator<T, TProperty> validator in _validators)
            {
                if (!validator.IsValid(instance, propertyValue))
                {
                    string errorMessage = validator.GetErrorMessage(_propertyName);
                    failures.Add(new ValidationFailure(_propertyName, errorMessage)
                    {
                        AttemptedValue = propertyValue
                    });
                }
            }

            return failures;
        }
    }

    /// <summary>
    /// Property validator interface
    /// </summary>
    internal interface IPropertyValidator<in T, in TProperty>
    {
        bool IsValid(T instance, TProperty value);
        string GetErrorMessage(string propertyName);
    }

}
