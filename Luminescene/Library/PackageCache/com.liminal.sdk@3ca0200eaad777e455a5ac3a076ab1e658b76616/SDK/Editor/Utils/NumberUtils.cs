using System;

namespace Liminal.SDK.Editor.Utils
{
    /// <summary>
    /// A collection of utilities for working with number types.
    /// </summary>
    internal static class NumberUtils
    {
        /// <summary>
        /// Returns the nubmer of digits in the supplied integer value.
        /// </summary>
        /// <param name="i">The integer value.</param>
        /// <returns>The number of digits in <paramref name="i"/></returns>
        public static int IntLength(int i)
        {
            if (i < 0)
                throw new ArgumentOutOfRangeException();

            if (i == 0)
                return 1;

            return (int)Math.Floor(Math.Log10(i)) + 1;
        }
    }
}
