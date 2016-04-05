namespace TBag.BloomFilters.Configurations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Count configuration with count type <see cref="long"/>.
    /// </summary>
    public class LongCountConfiguration : CountConfigurationBase<long>
    {
        /// <summary>
        /// Decrease the count
        /// </summary>
        public override Func<long,long> Decrease { get; set; } = DecreaseImpl;

        private static long DecreaseImpl(long c)
        {
            return c > long.MinValue ? c - 1 : long.MinValue;
        }

        /// <summary>
        /// The count identity value (0)
        /// </summary>
        public override Func<long> Identity { get; set; } = ()=>0L;

        /// <summary>
        /// Increase the count
        /// </summary>
        public override Func<long,long> Increase { get; set; } = IncreaseImpl;

        private static long IncreaseImpl(long c)
        {
            return c < long.MaxValue ? c + 1 : long.MaxValue;
        }

        /// <summary>
        /// Subtract counts
        /// </summary>
        public override Func<long,long,long> Subtract { get; set; } = SubtractImpl;

        private static long SubtractImpl(long c1, long c2)
        {
            try
            {
                return checked(c1 - c2);
            }
            catch (OverflowException)
            {
                return c2 > 0 ? int.MinValue : int.MaxValue;
            }
        }

        /// <summary>
        /// The count unity (1)
        /// </summary>
        public override Func<long> Unity { get; set; } = ()=>1L;

        /// <summary>
        /// Count comparer
        /// </summary>
        public override IComparer<long> Comparer { get; set; } = Comparer<long>.Default;

        /// <summary>
        /// Determine if the count is pure.
        /// </summary>
        public override Func<long, bool> IsPure { get; set; } = IsPureImpl;

        private static bool IsPureImpl(long c)
        {
            return c == 1L || c == -1L;
        }

        /// <summary>
        /// Add two counts
        /// </summary>
        public override Func<long,long,long> Add { get; set; } = AddImpl;

        private static long AddImpl(long c1, long c2)
        {
            try
            {
                return checked(c1 + c2);
            }
            catch (OverflowException)
            {
                return c2 > 0 ? int.MaxValue : int.MinValue;
            }
        }

        /// <summary>
        /// Determine if given the size of the Bloom filter, this count configuration is expected to be able to support the capacity.
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public override bool Supports(long capacity, long size)
        {
            return (long.MaxValue - 60) * size > capacity;
        }

        /// <summary>
        /// Get the estimated number of items in the Bloom filter.
        /// </summary>
        /// <param name="counts"></param>
        /// <param name="hashSize"></param>
        /// <returns></returns>
        public override long GetEstimatedCount(long[] counts, uint hashSize)
        {
            if (counts == null || hashSize <= 0) return 0L;
            return counts.Sum(c=>c==long.MinValue ? long.MaxValue : Math.Abs(c)) / hashSize;
        }
    }
}
