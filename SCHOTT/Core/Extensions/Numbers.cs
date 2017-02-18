using System;

namespace SCHOTT.Core.Extensions
{
    /// <summary>
    /// A class of double extensions.
    /// </summary>
    public static class DoubleExtensions
    {
        private const double DoubleTolerance = 0.001;

        /// <summary>
        /// Checks if a double is within a tolerance of an integer value.
        /// </summary>
        /// <param name="numberToTest">The double to test.</param>
        /// <param name="testValue">The integer to compare against.</param>
        /// <returns>True if testValue is within tolerance of numberToTest.</returns>
        public static bool EqualsInt(this double numberToTest, int testValue)
        {
            return Math.Abs(numberToTest - testValue) < DoubleTolerance;
        }

        /// <summary>
        /// Checks if a double is within a tolerance of an bool value.
        /// </summary>
        /// <param name="numberToTest">The double to test.</param>
        /// <param name="testValue">The bool to compare against.</param>
        /// <returns>True if testValue is within tolerance of numberToTest.</returns>
        public static bool EqualsBool(this double numberToTest, bool testValue)
        {
            return Math.Abs(numberToTest - (testValue ? 1 : 0)) < DoubleTolerance;
        }

        /// <summary>
        /// Checks if a double is within a tolerance of an double value.
        /// </summary>
        /// <param name="numberToTest">The double to test.</param>
        /// <param name="testValue">The double to compare against.</param>
        /// <returns>True if testValue is within tolerance of numberToTest.</returns>
        public static bool EqualsDouble(this double numberToTest, double testValue)
        {
            return Math.Abs(numberToTest - testValue) < DoubleTolerance;
        }
    }
}
