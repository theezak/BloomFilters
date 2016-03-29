namespace TBag.BloomFilters.Configurations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Count configuration
    /// </summary>
    public class IntCountConfiguration : CountConfigurationBase<int>
    {
        /// <summary>
        /// Decrease the count
        /// </summary>
        public override Func<int, int> Decrease { get; set; } = i => i- 1;

        /// <summary>
        /// The count identity value (0)
        /// </summary>
        public override Func<int> Identity { get; set; } = ()=>0;

        /// <summary>
        /// Increase the count
        /// </summary>
        public override Func<int, int> Increase { get; set; } = i => i + 1;

        /// <summary>
        /// Subtract counts
        /// </summary>
        public override Func<int, int, int> Subtract { get; set; } = (i1,i2)=>i1-i2;

        /// <summary>
        /// The count unity (1)
        /// </summary>
        public override Func<int> Unity { get; set; } = ()=>1;

        /// <summary>
        /// Count comparer
        /// </summary>
        public override IComparer<int> Comparer { get; set; } = Comparer<int>.Default;

        /// <summary>
        /// Determine if the count is pure.
        /// </summary>
        public override Func<int, bool> IsPure { get; set; } = i => Math.Abs(i) == 1;

        /// <summary>
        /// Add two counts
        /// </summary>
        public override Func<int, int, int> Add { get; set; } = (c1, c2) => c1 + c2;

        /// <summary>
        /// Determine if given the size of the Bloom filter, this count configuration is expected to be able to support the capacity.
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public override bool Supports(long capacity, long size)
        {
            return (int.MaxValue - 30) * size > capacity;
        }

        /// <summary>
        /// Get the estimated number of items in the Bloom filter.
        /// </summary>
        /// <param name="counts"></param>
        /// <param name="hashSize"></param>
        /// <returns></returns>
        public override long GetEstimatedCount(int[] counts, uint hashSize)
        {
            if (counts == null || hashSize <= 0) return 0L;
            return counts.Select(c => (long)c).Sum(c=>Math.Abs(c)) / hashSize;
        }
    }
}
