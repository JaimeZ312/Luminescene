using System;

namespace Liminal.SDK.Editor.Build
{
    /// <summary>
    /// Thrown when an invalid application id is supplied.
    /// </summary>
    public class InvalidAppException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="InvalidAppException"/>.
        /// </summary>
        /// <param name="message">The message for the exception.</param>
        public InvalidAppException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new <see cref="InvalidAppException"/>.
        /// </summary>
        /// <param name="id">The id that was supplied.</param>
        /// <param name="message">The message for the exception.</param>
        /// <param name="innerException">The inner exception.</param>
        public InvalidAppException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
