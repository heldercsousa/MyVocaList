using System;

namespace MyVocaList.Infra.Utils
{
    /// <summary>
    /// Guard clauses for parameter validation
    /// Use in repositories and services for fail-fast precondition checks
    /// </summary>
    public static class Guard
    {
        /// <summary>
        /// Throws ArgumentNullException if value is null
        /// </summary>
        public static void AgainstNull<T>(T value, string paramName) where T : class
        {
            if (value == null)
                throw new ArgumentNullException(paramName, $"'{paramName}' cannot be null.");
        }

        /// <summary>
        /// Throws ArgumentException if string is null, empty, or whitespace
        /// </summary>
        public static void AgainstNullOrWhiteSpace(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"'{paramName}' cannot be null, empty, or whitespace.", paramName);
        }

        /// <summary>
        /// Throws ArgumentException if value is negative or zero
        /// </summary>
        public static void AgainstNegativeOrZero(int value, string paramName)
        {
            if (value <= 0)
                throw new ArgumentException($"'{paramName}' must be greater than zero. Value: {value}", paramName);
        }

        /// <summary>
        /// Throws ArgumentOutOfRangeException if value is outside range
        /// </summary>
        public static void AgainstOutOfRange(int value, int min, int max, string paramName)
        {
            if (value < min || value > max)
                throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be between {min} and {max}.");
        }

        /// <summary>
        /// Throws ArgumentException if collection is null or empty
        /// </summary>
        public static void AgainstNullOrEmpty<T>(IEnumerable<T> collection, string paramName)
        {
            if (collection == null || !collection.Any())
                throw new ArgumentException($"'{paramName}' cannot be null or empty.", paramName);
        }

        /// <summary>
        /// Returns true if string is null, empty, or whitespace (non-throwing check)
        /// </summary>
        public static bool IsNullOrWhiteSpace(string value) => string.IsNullOrWhiteSpace(value);
    }
}
