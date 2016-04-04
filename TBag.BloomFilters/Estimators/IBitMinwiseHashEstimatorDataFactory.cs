namespace TBag.BloomFilters.Estimators
{
    /// <summary>
    /// Factory for creating <see cref="IBitMinwiseHashEstimatorData"/>.
    /// </summary>
    public interface IBitMinwiseHashEstimatorDataFactory
    {
        /// <summary>
        /// Create <see cref="IBitMinwiseHashEstimatorData"/>
        /// </summary>
        /// <param name="bitSize">The bit size</param>
        /// <param name="capacity">The capacity</param>
        /// <param name="hashCount">Number of hash functions to use</param>
        /// <returns></returns>
        IBitMinwiseHashEstimatorData Create(byte bitSize, long capacity, int hashCount);
    }
}