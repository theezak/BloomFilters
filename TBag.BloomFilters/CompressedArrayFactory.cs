namespace TBag.BloomFilters
{
    /// <summary>
    /// Factory for creating compressed arrays
    /// </summary>
    internal class CompressedArrayFactory : ICompressedArrayFactory
    {
        /// <summary>
        /// Create a compressed array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public ICompressedArray<T> Create<T>() where T : struct
        {
            return new CompressedArray<T>();
        }
    }
}
