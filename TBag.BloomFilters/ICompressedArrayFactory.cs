
namespace TBag.BloomFilters
{
    /// <summary>
    /// Interface for creating compressed arrays
    /// </summary>
    public interface ICompressedArrayFactory
    {
        /// <summary>
        /// Create a new compressed array
        /// </summary>
        /// <typeparam name="T">Array element type</typeparam>
        /// <returns></returns>
        ICompressedArray<T> Create<T>()
            where T : struct;
    }
}
