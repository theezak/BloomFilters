namespace TBag.BloomFilters.Countable.Configurations
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Base class for count configurations
    /// </summary>
    /// <typeparam name="TCount">The type of the counter.</typeparam>
    public abstract class CountConfigurationBase<TCount> : ICountConfiguration<TCount>
        where TCount : struct
    {
        /// <summary>
        /// The unity for the count type.
        /// </summary>
        public virtual TCount Unity { get; set; }

        /// <summary>
        /// Determine if the count is pure.
        /// </summary>
        public virtual Func<TCount, bool> IsPure { get; set; }

        /// <summary>
        /// Decrease the count
        /// </summary>
        public virtual Func<TCount, TCount> Decrease { get; set; }

        /// <summary>
        /// Count identity.
        /// </summary>
        public virtual TCount Identity { get; set; }

        /// <summary>
        /// Subtract two counts.
        /// </summary>
        public virtual Func<TCount, TCount, TCount> Subtract { get; set; }


        /// <summary>
        /// Increase the count.
        /// </summary>
        public virtual Func<TCount, TCount> Increase { get; set; }

        /// <summary>
        /// Count comparer
        /// </summary>
        public virtual IComparer<TCount> Comparer { get; set; }

        /// <summary>
        /// Determine if an IBF, given this configuration and the given <paramref name="capacity"/>, will support a set of the given size.
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="size"></param>
        /// <returns></returns>      
        public abstract bool Supports(long capacity, long size);

        /// <summary>
        /// Estimate the number of items in the Bloom filter.
        /// </summary>
        /// <param name="counts"></param>
        /// <param name="hashSize"></param>
        /// <returns></returns>
        public abstract long GetEstimatedCount(TCount[] counts, uint hashSize);

        /// <summary>
        /// The counter provider.
        /// </summary>
        public Func<ICompressedArray<TCount>> CounterProviderFactory { get; set; } = ()=>new CompressedArray<TCount>();

        /// <summary>
        /// Add two counts.
        /// </summary>
        public virtual Func<TCount, TCount, TCount> Add { get; set; }
    }
}
