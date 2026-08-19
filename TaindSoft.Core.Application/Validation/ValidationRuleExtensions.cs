namespace TaindSoft.Core.Application.Validation
{
    /// <summary>
    /// Common validation rule extensions
    /// </summary>
    public static class ValidationRuleExtensions
    {
        /// <summary>
        /// Defines a 'not null' validator on the current rule builder
        /// </summary>
        public static IRuleBuilder<T, TProperty> NotNull<T, TProperty>(
            this IRuleBuilder<T, TProperty> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new NotNullValidator<T, TProperty>());
        }

        /// <summary>
        /// Defines a 'not empty' validator on the current rule builder
        /// </summary>
        public static IRuleBuilder<T, TProperty> NotEmpty<T, TProperty>(
            this IRuleBuilder<T, TProperty> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new NotEmptyValidator<T, TProperty>());
        }

        /// <summary>
        /// Defines a 'length' validator on the current rule builder for strings
        /// </summary>
        public static IRuleBuilder<T, string> Length<T>(
            this IRuleBuilder<T, string> ruleBuilder,
            int min,
            int max)
        {
            return ruleBuilder.SetValidator(new LengthValidator<T>(min, max));
        }

        /// <summary>
        /// Defines a 'minimum length' validator on the current rule builder for strings
        /// </summary>
        public static IRuleBuilder<T, string> MinimumLength<T>(
            this IRuleBuilder<T, string> ruleBuilder,
            int minLength)
        {
            return ruleBuilder.SetValidator(new MinimumLengthValidator<T>(minLength));
        }

        /// <summary>
        /// Defines a 'maximum length' validator on the current rule builder for strings
        /// </summary>
        public static IRuleBuilder<T, string> MaximumLength<T>(
            this IRuleBuilder<T, string> ruleBuilder,
            int maxLength)
        {
            return ruleBuilder.SetValidator(new MaximumLengthValidator<T>(maxLength));
        }

        /// <summary>
        /// Defines an 'email address' validator on the current rule builder for strings
        /// </summary>
        public static IRuleBuilder<T, string> EmailAddress<T>(
            this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new EmailValidator<T>());
        }

        /// <summary>
        /// Defines a 'must' validator on the current rule builder
        /// </summary>
        public static IRuleBuilder<T, TProperty> Must<T, TProperty>(
            this IRuleBuilder<T, TProperty> ruleBuilder,
            Func<TProperty, bool> predicate)
        {
            return ruleBuilder.SetValidator(new PredicateValidator<T, TProperty>(predicate));
        }

        /// <summary>
        /// Defines a 'must' validator with instance context on the current rule builder
        /// </summary>
        public static IRuleBuilder<T, TProperty> Must<T, TProperty>(
            this IRuleBuilder<T, TProperty> ruleBuilder,
            Func<T, TProperty, bool> predicate)
        {
            return ruleBuilder.SetValidator(new PredicateWithInstanceValidator<T, TProperty>(predicate));
        }

        /// <summary>
        /// Defines a 'greater than' validator on the current rule builder
        /// </summary>
        public static IRuleBuilder<T, TProperty> GreaterThan<T, TProperty>(
            this IRuleBuilder<T, TProperty> ruleBuilder,
            TProperty valueToCompare)
            where TProperty : IComparable<TProperty>, IComparable
        {
            return ruleBuilder.SetValidator(new GreaterThanValidator<T, TProperty>(valueToCompare));
        }

        /// <summary>
        /// Defines a 'greater than or equal' validator on the current rule builder
        /// </summary>
        public static IRuleBuilder<T, TProperty> GreaterThanOrEqualTo<T, TProperty>(
            this IRuleBuilder<T, TProperty> ruleBuilder,
            TProperty valueToCompare)
            where TProperty : IComparable<TProperty>, IComparable
        {
            return ruleBuilder.SetValidator(new GreaterThanOrEqualValidator<T, TProperty>(valueToCompare));
        }

        /// <summary>
        /// Defines a 'less than' validator on the current rule builder
        /// </summary>
        public static IRuleBuilder<T, TProperty> LessThan<T, TProperty>(
            this IRuleBuilder<T, TProperty> ruleBuilder,
            TProperty valueToCompare)
            where TProperty : IComparable<TProperty>, IComparable
        {
            return ruleBuilder.SetValidator(new LessThanValidator<T, TProperty>(valueToCompare));
        }

        /// <summary>
        /// Defines a 'less than or equal' validator on the current rule builder
        /// </summary>
        public static IRuleBuilder<T, TProperty> LessThanOrEqualTo<T, TProperty>(
            this IRuleBuilder<T, TProperty> ruleBuilder,
            TProperty valueToCompare)
            where TProperty : IComparable<TProperty>, IComparable
        {
            return ruleBuilder.SetValidator(new LessThanOrEqualValidator<T, TProperty>(valueToCompare));
        }

        /// <summary>
        /// Defines an 'equal' validator on the current rule builder
        /// </summary>
        public static IRuleBuilder<T, TProperty> Equal<T, TProperty>(
            this IRuleBuilder<T, TProperty> ruleBuilder,
            TProperty valueToCompare)
        {
            return ruleBuilder.SetValidator(new EqualValidator<T, TProperty>(valueToCompare));
        }

        /// <summary>
        /// Defines a 'not equal' validator on the current rule builder
        /// </summary>
        public static IRuleBuilder<T, TProperty> NotEqual<T, TProperty>(
            this IRuleBuilder<T, TProperty> ruleBuilder,
            TProperty valueToCompare)
        {
            return ruleBuilder.SetValidator(new NotEqualValidator<T, TProperty>(valueToCompare));
        }
    }

}
