namespace TBag.BloomFilters.Estimators
{
    /// <summary>
    /// Interface for bit minwise hash estimator data.
    /// </summary>
    public interface IBitMinwiseHashEstimatorFullData
    {
        /// <summary>
        /// The number of bits.
        /// </summary>
        byte BitSize { get; set; }

        /// <summary>
        /// The capacity (number of elements).
        /// </summary>
        ulong Capacity { get; set; }

        /// <summary>
        /// The number of hash functions
        /// </summary>
        int HashCount { get; set; }

        /// <summary>
        /// The hashed values.
        /// </summary>
        int[] Values { get; set; }
    }
}