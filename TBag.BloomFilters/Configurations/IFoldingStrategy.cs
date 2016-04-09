using System;
using System.Collections.Generic;

namespace TBag.BloomFilters.Configurations
{
    /// <summary>
    /// Strategy for sizing Bloom filters so they are foldable and then finding a fold that does not violate the key count.
    /// </summary>
    public interface IFoldingStrategy
    {
        /// <summary>
        /// Compute a foldable size larger than <paramref name="size"/> that has <paramref name="foldFactor"/> as a factor (if at least 1).
        /// </summary>
        /// <param name="size"></param>
        /// <param name="foldFactor"></param>
        /// <returns></returns>
        long  ComputeFoldableSize(long size, int foldFactor);

        /// <summary>
        /// Find a fold factor for a Bloom filter of <paramref name="blockSize"/> and given <paramref name="capacity"/> to match the <paramref name="keyCount"/>.
        /// </summary>
        /// <param name="blockSize">The size of the Bloom filter.</param>
         /// <param name="capacity">The current capacity.</param>
        /// <param name="keyCount">The actual number of keys.</param>
        /// <returns>A compression factor</returns>
        uint? FindCompressionFactor(long blockSize, long capacity, long? keyCount = null);

        /// <summary>
        /// Get the best fold factor for two Bloom filters of <paramref name="blockSize1"/> and <paramref name="blockSize2"/> size.
        /// </summary>
        /// <param name="blockSize1"></param>
        /// <param name="blockSize2"></param>
        /// <returns>Fold factors to make the bloom Filters equal size.</returns>
        Tuple<long, long> GetFoldFactors(long blockSize1, long blockSize2);

        /// <summary>
        /// Get all possible fold factors for the given <paramref name="blockSize"/>.
        /// </summary>
        /// <param name="blockSize"></param>
        /// <returns></returns>
        IEnumerable<long> GetAllFoldFactors(long blockSize);
    }
}