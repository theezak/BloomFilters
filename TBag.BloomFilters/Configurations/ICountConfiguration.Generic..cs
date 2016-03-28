namespace TBag.BloomFilters.Configurations
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
        Func<TCount> Unity { get; set; }

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
        Func<TCount> Identity { get; set; }

        /// <summary>
        /// Subtract two counts.
        /// </summary>
        Func<TCount, TCount, TCount> Subtract { get; set; }

        /// <summary>
        /// Add two counts
        /// </summary>
        Func<TCount,TCount,TCount> Add { get; set; }

        /// <summary>
        /// Increase the count.
        /// </summary>
        Func<TCount, TCount> Increase { get; set; }

        /// <summary>
        /// Equality comparer for counts.
        /// </summary>
        IEqualityComparer<TCount> EqualityComparer { get; set; }
    }
}
