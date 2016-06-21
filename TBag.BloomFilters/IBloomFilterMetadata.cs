
namespace TBag.BloomFilters
{
    public interface IBloomFilterMetadata
    {
        /// <summary>
        /// The block size
        /// </summary>
        /// <remarks>The block size is the actual size of the array with data</remarks>
        long BlockSize { get; set; }

        /// <summary>
        /// The capacity
        /// </summary>
        /// <remarks>The capacity (number of items) the Bloom filter was sized for. You can exceed the capacity, but it results in exceeding the error rate as well.</remarks>
        long Capacity { get; set; }

        /// <summary>
        /// The number of hash functions used
        /// </summary>
        uint HashFunctionCount { get; set; }

        /// <summary>
        /// Total number of items stored in the Bloom filter
        /// </summary>
        /// <remarks>Dependending upon the operations that occured, thus can be an estimate.</remarks>
        long ItemCount { get; set; }
    }
}
