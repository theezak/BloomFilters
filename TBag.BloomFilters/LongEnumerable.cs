namespace TBag.BloomFilters
{
    using System.Collections.Generic;

    /// <summary>
    /// Long enumerable.
    /// </summary>
    internal static class LongEnumerable
    {
        /// <summary>
        /// Generate a range of type <see cref="long"/>.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        internal static IEnumerable<long> Range(long start, long end)
        {
            for (var i = start; i < end; i++)
                yield return i;
        }
    }
}
