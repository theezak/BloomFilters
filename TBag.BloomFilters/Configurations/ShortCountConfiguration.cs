using System.Linq;

namespace TBag.BloomFilters.Configurations
{
    using System;
    using System.Collections.Generic;
   
    /// <summary>
    /// Count configuration for type <see cref="short"/>.
    /// </summary>
    public class ShortCountConfiguration : CountConfigurationBase<short>
    {
        /// <summary>
        /// Decrease the count
        /// </summary>
        public override Func<short, short> Decrease { get; set; } =  sb => (short)(sb - 1);

        /// <summary>
        /// Identity for the count (0).
        /// </summary>
        public override Func<short> Identity { get; set; } = ()=>0;

        /// <summary>
        /// Increase the count
        /// </summary>
        public override Func<short, short> Increase { get; set; } = sb => (short)(sb + 1);

        /// <summary>
        /// Subtract two count values
        /// </summary>
        public override Func<short, short, short> Subtract { get; set; } = (sb1,sb2) => (short)(sb1 - sb2);

        /// <summary>
        /// Unity of the count (1).
        /// </summary>
        public override Func<short> Unity { get; set; } = ()=>1;

        /// <summary>
        /// Comparer for the count
        /// </summary>
        public override IComparer<short>Comparer { get; set; } = Comparer<short>.Default;

        /// <summary>
        /// Determine if the count is pure.
        /// </summary>
        public override Func<short, bool> IsPure { get; set; } = sb => Math.Abs(sb) == 1;

        /// <summary>
        /// Add two counts
        /// </summary>
        public override Func<short, short, short> Add { get; set; } = (c1, c2) => (short)(c1 + c2);

        /// <summary>
        /// Determine if given the size of the Bloom filter, this count configuration is expected to be able to support the capacity.
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public override bool Supports(long capacity, long size)
        {
            return (short.MaxValue - 20) * size > capacity;
        }

        /// <summary>
        /// Get the estimated number of items in the Bloom filter.
        /// </summary>
        /// <param name="counts"></param>
        /// <param name="hashSize"></param>
        /// <returns></returns>
        public override long GetEstimatedCount(short[] counts, uint hashSize)
        {
            if (counts == null || hashSize <= 0) return 0L;
            return counts.Select(c => (long)c).Sum(c => Math.Abs(c)) / hashSize;
        }
    }
}
