using System;

namespace TBag.BloomFilters.Configurations
{
    /// <summary>
    /// Strategy for sizing Bloom filters so they are foldable and then finding a fold that does not violate the key count.
    /// </summary>
    public interface IFoldingStrategy
    {
        long  ComputeFoldableSize(long size, int foldFactor);

        /// <summary>
        /// Find a good folding factor.
        /// </summary>
        /// <param name="blockSize">The size of the Bloom filter.</param>
         /// <param name="capacity">The current capacity.</param>
        /// <param name="keyCount">The actual number of keys.</param>
        /// <returns></returns>
        uint? FindFoldFactor(long blockSize, long capacity, long? keyCount = null);

        /// <summary>
        /// Get the best matching fold factor to make the two sizes match .
        /// </summary>
        /// <param name="size1"></param>
        /// <param name="size2"></param>
        /// <returns></returns>
        Tuple<long, long> GetFoldFactors(long size1, long size2);
    }
}