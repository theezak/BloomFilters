
namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provide counters for a Bloom filter.
    /// </summary>
    /// <typeparam name="TCount">The counter type</typeparam>
    public interface ICompressedArray<TCount> : IEnumerable<TCount>
        where TCount : struct
    {
        /// <summary>
        /// Indexer
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        TCount this[long index] { get; set; }

        /// <summary>
        /// Load the counters.
        /// </summary>
        /// <param name="counters">The counters</param>
        /// <param name="blockSize">Block size</param>
        /// <param name="membershipTest">Optional member ship test</param>
        /// <remarks>When <see cref="membershipTest"/> is null and the counters are the same length as the block size, an array will be used. Otherwise, a dictionary will be used with a sparse array based upon membership (removing 0 count). The membership could be tested against for example the idSum or hashSum (when not 0, a count should exist)</remarks>
        void Load(TCount[] counters, long blockSize, Func<long, bool> membershipTest = null);
    }
}