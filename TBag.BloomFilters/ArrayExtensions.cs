namespace TBag.BloomFilters
{
    using System;
    /// <summary>
    /// Array extensions.
    /// </summary>
    internal static class ArrayExtensions
    {
        /// <summary>
        /// Get the folded value at the given position
        /// </summary>
        /// <typeparam name="T">Type of the item</typeparam>
        /// <param name="values">The values</param>
        /// <param name="position">The position</param>
        /// <param name="foldFactor">Factor to fold by</param>
        /// <param name="foldOperator">The operator to apply during folding</param>
        /// <returns></returns>
        internal static T GetFolded<T>(this T[] values, long position, long? foldFactor, Func<T, T, T> foldOperator)
        {
            if (values == null) return default(T);
            if ((foldFactor ?? 0L) <= 1L) return values[position];
            var foldedSize = values.Length / foldFactor.Value;
            position = position % foldedSize;
            var val = values[position];
            foldFactor--;
            position += foldedSize;
            while (foldFactor > 0)
            {
                val = foldOperator(val, values[position]);
                foldFactor--;
                position += foldedSize;
            }
            return val;
        }
    }
}
