namespace TBag.BloomFilters.Configurations
{
    using System;
    using System.Collections.Generic;
    /// <summary>
    /// Configuration for the counter.
    /// </summary>
    /// <typeparam name="TCount"></typeparam>
    public interface ICountConfiguration<TCount>
        where TCount : struct
    {
        /// <summary>
        /// The unity for the count type.
        /// </summary>
        Func<TCount> CountUnity { get; set; }

        /// <summary>
        /// Determine if the count is pure.
        /// </summary>
        Func<TCount, bool> IsPureCount { get; set; }

        /// <summary>
        /// Decrease the count
        /// </summary>
        Func<TCount, TCount> CountDecrease { get; set; }

        /// <summary>
        /// Count identity.
        /// </summary>
        Func<TCount> CountIdentity { get; set; }

        /// <summary>
        /// Subtract two counts.
        /// </summary>
        Func<TCount, TCount, TCount> CountSubtract { get; set; }


        /// <summary>
        /// Increase the count.
        /// </summary>
        Func<TCount, TCount> CountIncrease { get; set; }

        /// <summary>
        /// Equality comparer for counts.
        /// </summary>
        IEqualityComparer<TCount> EqualityComparer { get; set; }
    }
}
