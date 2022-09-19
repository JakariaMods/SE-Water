using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jakaria.Utils
{
    /// <summary>
    /// Basic Assertion library that throws an exception when a tested condition fails
    /// </summary>
    public static class Assert
    {
        /// <summary>
        /// Asserts when the condition is false
        /// </summary>
        public static void True(bool condition, string message = null)
        {
            if (!condition)
            {
                throw new AssertionFailedException(message);
            }
        }

        /// <summary>
        /// Asserts when the condition is true
        /// </summary>
        public static void False(bool condition, string message = null)
        {
            True(!condition, message);
        }

        /// <summary>
        /// Asserts when the provided object is not null
        /// </summary>
        public static void Null(object condition, string message = null)
        {
            True(condition == null, message);
        }

        /// <summary>
        /// Asserts when the provided object is null
        /// </summary>
        public static void NotNull(object condition, string message = null)
        {
            True(condition != null, message);
        }

        private class AssertionFailedException : Exception
        {
            public override string Message => _message;

            private readonly string _message;

            public AssertionFailedException(string message = null)
            {
                if(message == null)
                {
                    _message = "An assertion failed";
                }
                else
                {
                    _message = message;
                }
            }
        }
    }
}
