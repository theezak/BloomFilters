
namespace TBag.BloomFilters
{
    using System;

    internal static class CompressedArrayExtensions
    {
        /// <summary>
        /// Get the folded value at the given position
        /// </summary>
        /// <typeparam name="TCount">Type of the item</typeparam>
        /// <param name="values">The values</param>
        /// <param name="position">The position</param>
        /// <param name="blockSize"></param>
        /// <param name="foldFactor">Factor to fold by</param>
        /// <param name="foldOperator">The operator to apply during folding</param>
        /// <returns></returns>
        internal static TCount GetFolded<TCount>(
            this ICompressedArray<TCount> values,
            long position,
            long blockSize,
            long? foldFactor,
            Func<TCount, TCount, TCount> foldOperator)
            where TCount : struct
        {
            if (values == null) return default(TCount);
            if ((foldFactor ?? 0L) <= 1L) return values[position];
            var foldedSize = blockSize / foldFactor.Value;
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
