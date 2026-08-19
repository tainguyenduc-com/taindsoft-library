using System.Collections;
using System.Text.RegularExpressions;

namespace TaindSoft.Core.Application.Validation
{
    /// <summary>
    /// Not null validator
    /// </summary>
    public class NotNullValidator<T, TProperty> : PropertyValidatorBase<T, TProperty>
    {
        protected override string DefaultErrorMessage => "{PropertyName} must not be null";

        public override bool IsValid(T instance, TProperty value)
        {
            return value != null;
        }
    }

    /// <summary>
    /// Not empty validator
    /// </summary>
    public class NotEmptyValidator<T, TProperty> : PropertyValidatorBase<T, TProperty>
    {
        protected override string DefaultErrorMessage => "{PropertyName} must not be empty";

        public override bool IsValid(T instance, TProperty value)
        {
            if (value == null)
            {
                return false;
            }

            if (value is string str)
            {
                return !string.IsNullOrWhiteSpace(str);
            }

            if (value is IEnumerable enumerable)
            {
                return enumerable.GetEnumerator().MoveNext();
            }

            return !Equals(value, default(TProperty));
        }
    }

    /// <summary>
    /// Length validator for strings
    /// </summary>
    public class LengthValidator<T>(int min, int max) : PropertyValidatorBase<T, string>
    {
        private readonly int _min = min;
        private readonly int _max = max;

        protected override string DefaultErrorMessage =>
            $"{{PropertyName}} must be between {_min} and {_max} characters";

        public override bool IsValid(T instance, string value)
        {
            if (value == null)
            {
                return true;
            }

            int length = value.Length;
            return length >= _min && length <= _max;
        }
    }

    /// <summary>
    /// Minimum length validator
    /// </summary>
    public class MinimumLengthValidator<T>(int minLength) : PropertyValidatorBase<T, string>
    {
        private readonly int _minLength = minLength;

        protected override string DefaultErrorMessage =>
            $"{{PropertyName}} must be at least {_minLength} characters";

        public override bool IsValid(T instance, string value)
        {
            if (value == null)
            {
                return true;
            }

            return value.Length >= _minLength;
        }
    }

    /// <summary>
    /// Maximum length validator
    /// </summary>
    public class MaximumLengthValidator<T>(int maxLength) : PropertyValidatorBase<T, string>
    {
        private readonly int _maxLength = maxLength;

        protected override string DefaultErrorMessage =>
            $"{{PropertyName}} must not exceed {_maxLength} characters";

        public override bool IsValid(T instance, string value)
        {
            if (value == null)
            {
                return true;
            }

            return value.Length <= _maxLength;
        }
    }

    /// <summary>
    /// Email address validator
    /// </summary>
    public partial class EmailValidator<T> : PropertyValidatorBase<T, string>
    {
        private static readonly Regex EmailRegex = RegexDefine.EmailRegex();

        protected override string DefaultErrorMessage => "{PropertyName} is not a valid email address";

        public override bool IsValid(T instance, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            return EmailRegex.IsMatch(value);
        }

    }

    /// <summary>
    /// Predicate validator
    /// </summary>
    public class PredicateValidator<T, TProperty>(Func<TProperty, bool> predicate) : PropertyValidatorBase<T, TProperty>
    {
        private readonly Func<TProperty, bool> _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));

        protected override string DefaultErrorMessage => "{PropertyName} is not valid";

        public override bool IsValid(T instance, TProperty value)
        {
            return _predicate(value);
        }
    }

    /// <summary>
    /// Predicate validator with instance context
    /// </summary>
    public class PredicateWithInstanceValidator<T, TProperty>(Func<T, TProperty, bool> predicate) : PropertyValidatorBase<T, TProperty>
    {
        private readonly Func<T, TProperty, bool> _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));

        protected override string DefaultErrorMessage => "{PropertyName} is not valid";

        public override bool IsValid(T instance, TProperty value)
        {
            return _predicate(instance, value);
        }
    }

    /// <summary>
    /// Greater than validator
    /// </summary>
    public class GreaterThanValidator<T, TProperty>(TProperty valueToCompare) : PropertyValidatorBase<T, TProperty>
        where TProperty : IComparable<TProperty>, IComparable
    {
        private readonly TProperty _valueToCompare = valueToCompare;

        protected override string DefaultErrorMessage =>
            $"{{PropertyName}} must be greater than {_valueToCompare}";

        public override bool IsValid(T instance, TProperty value)
        {
            if (value == null)
            {
                return true;
            }

            return value.CompareTo(_valueToCompare) > 0;
        }
    }

    /// <summary>
    /// Greater than or equal validator
    /// </summary>
    public class GreaterThanOrEqualValidator<T, TProperty>(TProperty valueToCompare) : PropertyValidatorBase<T, TProperty>
        where TProperty : IComparable<TProperty>, IComparable
    {
        private readonly TProperty _valueToCompare = valueToCompare;

        protected override string DefaultErrorMessage =>
            $"{{PropertyName}} must be greater than or equal to {_valueToCompare}";

        public override bool IsValid(T instance, TProperty value)
        {
            if (value == null)
            {
                return true;
            }

            return value.CompareTo(_valueToCompare) >= 0;
        }
    }

    /// <summary>
    /// Less than validator
    /// </summary>
    public class LessThanValidator<T, TProperty>(TProperty valueToCompare) : PropertyValidatorBase<T, TProperty>
        where TProperty : IComparable<TProperty>, IComparable
    {
        private readonly TProperty _valueToCompare = valueToCompare;

        protected override string DefaultErrorMessage =>
            $"{{PropertyName}} must be less than {_valueToCompare}";

        public override bool IsValid(T instance, TProperty value)
        {
            if (value == null)
            {
                return true;
            }

            return value.CompareTo(_valueToCompare) < 0;
        }
    }

    /// <summary>
    /// Less than or equal validator
    /// </summary>
    public class LessThanOrEqualValidator<T, TProperty>(TProperty valueToCompare) : PropertyValidatorBase<T, TProperty>
        where TProperty : IComparable<TProperty>, IComparable
    {
        private readonly TProperty _valueToCompare = valueToCompare;

        protected override string DefaultErrorMessage =>
            $"{{PropertyName}} must be less than or equal to {_valueToCompare}";

        public override bool IsValid(T instance, TProperty value)
        {
            if (value == null)
            {
                return true;
            }

            return value.CompareTo(_valueToCompare) <= 0;
        }
    }

    /// <summary>
    /// Equal validator
    /// </summary>
    public class EqualValidator<T, TProperty>(TProperty valueToCompare) : PropertyValidatorBase<T, TProperty>
    {
        private readonly TProperty _valueToCompare = valueToCompare;

        protected override string DefaultErrorMessage =>
            $"{{PropertyName}} must be equal to {_valueToCompare}";

        public override bool IsValid(T instance, TProperty value)
        {
            return Equals(value, _valueToCompare);
        }
    }

    /// <summary>
    /// Not equal validator
    /// </summary>
    public class NotEqualValidator<T, TProperty>(TProperty valueToCompare) : PropertyValidatorBase<T, TProperty>
    {
        private readonly TProperty _valueToCompare = valueToCompare;

        protected override string DefaultErrorMessage =>
            $"{{PropertyName}} must not be equal to {_valueToCompare}";

        public override bool IsValid(T instance, TProperty value)
        {
            return !Equals(value, _valueToCompare);
        }
    }

}
