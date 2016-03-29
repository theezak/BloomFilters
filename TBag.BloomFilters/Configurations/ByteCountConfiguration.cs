using System.Linq;

namespace TBag.BloomFilters.Configurations
{
    using System;
    using System.Collections.Generic;
   
    /// <summary>
    /// Count configuration for type <see cref="sbyte"/>.
    /// </summary>
    public class ByteCountConfiguration : CountConfigurationBase<sbyte>
    {
        /// <summary>
        /// Decrease the count
        /// </summary>
        public override Func<sbyte, sbyte> Decrease { get; set; } =  sb => (sbyte)(sb - 1);

        /// <summary>
        /// Identity for the count (0).
        /// </summary>
        public override Func<sbyte> Identity { get; set; } = ()=>0;

        /// <summary>
        /// Increase the count
        /// </summary>
        public override Func<sbyte, sbyte> Increase { get; set; } = sb => (sbyte)(sb + 1);

        /// <summary>
        /// Subtract two count values
        /// </summary>
        public override Func<sbyte, sbyte, sbyte> Subtract { get; set; } = (sb1,sb2) => (sbyte)(sb1 - sb2);

        /// <summary>
        /// Unity of the count (1).
        /// </summary>
        public override Func<sbyte> Unity { get; set; } = ()=>1;

        /// <summary>
        /// Comparer for the count
        /// </summary>
        public override IComparer<sbyte> Comparer { get; set; } = Comparer<sbyte>.Default;

        /// <summary>
        /// Determine if given the size of the Bloom filter, this count configuration is expected to be able to support the capacity.
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public override bool Supports(long capacity, long size)
        {
            return (sbyte.MaxValue - 15) * size > capacity;
        }

        //?Determine the estimated number of items in the Bloom filter.
        public override long GetEstimatedCount(sbyte[] counts, uint hashSize)
        {
            if (counts == null || hashSize <= 0) return 0L;
            return counts.Select(c => (long) c).Sum(c=>Math.Abs(c))/hashSize;
        }

        /// <summary>
        /// Determine if the count is pure.
        /// </summary>
        public override Func<sbyte, bool> IsPure { get; set; } = sb => Math.Abs(sb) == 1;

        /// <summary>
        /// Add two counts.
        /// </summary>
        public override  Func<sbyte, sbyte, sbyte> Add { get; set; } = (sb1, sb2) => (sbyte)(sb1 + sb2);
        
    }
}
