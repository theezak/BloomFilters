namespace TBag.BloomFilters.Configurations
{
    using System;
    using System.Collections.Generic;

    public abstract class CountConfigurationBase<TCount> : ICountConfiguration<TCount>
        where TCount : struct
    {
        /// <summary>
        /// The unity for the count type.
        /// </summary>
        public virtual Func<TCount> Unity { get; set; }

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
        public virtual Func<TCount> Identity { get; set; }

        /// <summary>
        /// Subtract two counts.
        /// </summary>
        public virtual Func<TCount, TCount, TCount> Subtract { get; set; }


        /// <summary>
        /// Increase the count.
        /// </summary>
        public virtual Func<TCount, TCount> Increase { get; set; }

        /// <summary>
        /// Count equality comparer
        /// </summary>
        public virtual IEqualityComparer<TCount> EqualityComparer { get; set; }

        /// <summary>
        /// Add two counts.
        /// </summary>
        public virtual Func<TCount, TCount, TCount> Add { get; set; }
    }
}
