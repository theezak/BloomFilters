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
         /// <param name="hashFunctionCount">The number of hash functions</param>
        /// <param name="keyCount">The actual number of keys.</param>
        /// <returns></returns>
        uint? FindFoldFactor(long blockSize, long capacity, long? keyCount = null);
    }
}