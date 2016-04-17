namespace TBag.BloomFilters
{
    using System;
    using System.Linq;
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
        internal static T GetFolded<T>(
            this T[] values, 
            long position, 
            long foldFactor, 
            Func<T, T, T> foldOperator)
        {
             if (foldFactor <= 1L) return values[position];
            var foldedSize = values.Length / foldFactor;
            position = position % foldedSize;
            return LongEnumerable.Range(1L, foldFactor)
                .Aggregate(values[position],
                    (foldedValue, factor) => foldOperator(foldedValue, values[position + factor * foldedSize]));
        }
    }
}
