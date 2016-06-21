namespace TBag.BloomFilters.Countable.Configurations
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Configuration for the counter.
    /// </summary>
    /// <typeparam name="TCount">The type of the counter</typeparam>
    public interface ICountConfiguration<TCount>
        where TCount : struct
    {
        /// <summary>
        /// The unity for the count type.
        /// </summary>
        TCount Unity { get; set; }

        /// <summary>
        /// Determine if the count is pure.
        /// </summary>
        Func<TCount, bool> IsPure { get; set; }

        /// <summary>
        /// Decrease the count
        /// </summary>
        Func<TCount, TCount> Decrease { get; set; }

        /// <summary>
        /// Count identity.
        /// </summary>
       TCount Identity { get; set; }

        /// <summary>
        /// Subtract two counts.
        /// </summary>
        Func<TCount, TCount, TCount> Subtract { get; set; }

        /// <summary>
        /// Add two counts
        /// </summary>
        Func<TCount, TCount, TCount> Add { get; set; }

        /// <summary>
        /// Increase the count.
        /// </summary>
        Func<TCount, TCount> Increase { get; set; }

        /// <summary>
        /// A comparer for the count.
        /// </summary>
        IComparer<TCount> Comparer { get; set; }

        /// <summary>
        /// Determine if, given the size of the Bloom filter, this count configuration is expected to be able to support the capacity.
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        bool Supports(long capacity, long size);

        /// <summary>
        /// Estimate the number of items in the filter.
        /// </summary>
        /// <param name="counts"></param>
        /// <param name="hashSize"></param>
        /// <returns></returns>
        long GetEstimatedCount(TCount[] counts, uint hashSize);

    }
}
