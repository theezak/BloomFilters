namespace TBag.BloomFilters.Standard
{
    /// <summary>
    /// Interface for Bloom filter data
    /// </summary>
    public interface IBloomFilterData : IBloomFilterMetadata
    {
        /// <summary>
        /// The boolean values for the Bloom filter (as a byte array)
        /// </summary>
        byte[] Bits { get; set; }
    }
}