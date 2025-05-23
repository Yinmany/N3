using System;

namespace N3
{
    public static class RandomHelper
    {
#if NET6_0_OR_GREATER



#else


        /// <summary>
        ///   Performs an in-place shuffle of an array.
        /// </summary>
        /// <param name="values">The array to shuffle.</param>
        /// <typeparam name="T">The type of array.</typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="values" /> is <see langword="null" />.</exception>
        /// <remarks>
        ///   This method uses <see cref="Next(int, int)" /> to choose values for shuffling.
        ///   This method is an O(n) operation.
        /// </remarks>
        public static void Shuffle<T>(this Random self, T[] values)
        {
            if (values is null)
                throw new ArgumentNullException(nameof(values));
            self.Shuffle(values.AsSpan());
        }

        /// <summary>
        ///   Performs an in-place shuffle of a span.
        /// </summary>
        /// <param name="values">The span to shuffle.</param>
        /// <typeparam name="T">The type of span.</typeparam>
        /// <remarks>
        ///   This method uses <see cref="Next(int, int)" /> to choose values for shuffling.
        ///   This method is an O(n) operation.
        /// </remarks>
        public static void Shuffle<T>(this Random self, Span<T> values)
        {
            int n = values.Length;

            for (int i = 0; i < n - 1; i++)
            {
                int j = self.Next(i, n);

                if (j != i)
                {
                    T temp = values[i];
                    values[i] = values[j];
                    values[j] = temp;
                }
            }
        }
#endif
    }
}
