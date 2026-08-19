namespace TaindSoft.Core.Domain.Guards
{
    /// <summary>
    /// Guard clauses for defensive programming
    /// </summary>
    public static class Guard
    {
        public static void AgainstNull<T>(T? value, string parameterName) where T : class
        {
            if (value is null)
            {
                throw new ArgumentNullException(parameterName, $"{parameterName} cannot be null");
            }
        }

        public static void AgainstNullOrEmpty(string? value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{parameterName} cannot be null or empty", parameterName);
            }
        }

        public static void AgainstOutOfRange<T>(T value, T min, T max, string parameterName)
            where T : IComparable
        {
            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            {
                throw new ArgumentOutOfRangeException(parameterName,
                    $"{parameterName} must be between {min} and {max}");
            }
        }

        public static void AgainstZero(int value, string parameterName)
        {
            if (value == 0)
            {
                throw new ArgumentException($"{parameterName} cannot be zero", parameterName);
            }
        }

        public static void AgainstNegative(decimal value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentException($"{parameterName} cannot be negative", parameterName);
            }
        }

        public static void AgainstInvalidEmail(string? email, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                throw new ArgumentException($"{parameterName} is not a valid email", parameterName);
            }
        }
    }
}
