namespace TBag.BloomFilters.Configurations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Count configuration for type <see cref="short"/>.
    /// </summary>
    public class ShortCountConfiguration : CountConfigurationBase<short>
    {
        /// <summary>
        /// Decrease the count
        /// </summary>
        public override Func<short, short> Decrease { get; set; } =  DecreaseImpl;

        private static short DecreaseImpl(short c)
        {
            return c == short.MinValue ? short.MinValue : (short)(c - 1);
        }

        /// <summary>
        /// Identity for the count (0).
        /// </summary>
        public override Func<short> Identity { get; set; } = ()=>0;

        /// <summary>
        /// Increase the count
        /// </summary>
        public override Func<short, short> Increase { get; set; } = IncreaseImpl;

        private static short IncreaseImpl(short c)
        {
            return c == short.MaxValue ? short.MaxValue : (short)(c + 1);
        }

        /// <summary>
        /// Subtract two count values
        /// </summary>
        public override Func<short, short, short> Subtract { get; set; } = SubtractImpl;

        private static short SubtractImpl(short c1, short c2)
        {
            var res = (long)c1 - c2;
            if (res > short.MaxValue) return short.MaxValue;
            if (res < short.MinValue) return short.MinValue;
            return (short)res;
        }

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
        public override Func<short, bool> IsPure { get; set; } = IsPureImpl;

        private static bool IsPureImpl(short c)
        {
            return c == 1 || c == -1;
        }

        /// <summary>
        /// Add two counts
        /// </summary>
        public override Func<short, short, short> Add { get; set; } = AddImpl;

        private static short AddImpl(short c1, short c2)
        {
            var res = (long)c1 + c2;
            if (res > short.MaxValue) return short.MaxValue;
            if (res < short.MinValue) return short.MinValue;
            return (short)res;
        }

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
