namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;

    public abstract class CountConfiguration<TCount> : ICountConfiguration<TCount>
        where TCount : struct
    {
        /// <summary>
        /// The unity for the count type.
        /// </summary>
        public virtual Func<TCount> CountUnity { get; set; }

        /// <summary>
        /// Determine if the count is pure.
        /// </summary>
        public virtual Func<TCount, bool> IsPureCount { get; set; }

        /// <summary>
        /// Decrease the count
        /// </summary>
        public virtual Func<TCount, TCount> CountDecrease { get; set; }

        /// <summary>
        /// Count identity.
        /// </summary>
        public virtual Func<TCount> CountIdentity { get; set; }

        /// <summary>
        /// Subtract two counts.
        /// </summary>
        public virtual Func<TCount, TCount, TCount> CountSubtract { get; set; }


        /// <summary>
        /// Increase the count.
        /// </summary>
        public virtual Func<TCount, TCount> CountIncrease { get; set; }

        /// <summary>
        /// Count equality comparer
        /// </summary>
        public virtual IEqualityComparer<TCount> EqualityComparer { get; set; }
    }
}
