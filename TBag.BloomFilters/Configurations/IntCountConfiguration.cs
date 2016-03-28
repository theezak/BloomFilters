namespace TBag.BloomFilters.Configurations
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Count configuration
    /// </summary>
    public class IntCountConfiguration : ICountConfiguration<int>
    {
        /// <summary>
        /// Decrease the count
        /// </summary>
        public Func<int, int> CountDecrease { get; set; } = i => i- 1;

        /// <summary>
        /// The count identity value (0)
        /// </summary>
        public Func<int> CountIdentity { get; set; } = ()=>0;

        /// <summary>
        /// Increase the count
        /// </summary>
        public Func<int, int> CountIncrease { get; set; } = i => i + 1;

        /// <summary>
        /// Subtract counts
        /// </summary>
        public Func<int, int, int> CountSubtract { get; set; } = (i1,i2)=>i1-i2;

        /// <summary>
        /// The count unity (1)
        /// </summary>
        public Func<int> CountUnity { get; set; } = ()=>1;

        /// <summary>
        /// Count equality comparer
        /// </summary>
        public IEqualityComparer<int> EqualityComparer { get; set; } = EqualityComparer<int>.Default;

        /// <summary>
        /// Determine if the count is pure.
        /// </summary>
        public Func<int, bool> IsPureCount { get; set; } = i => Math.Abs(i) == 1;
    }
}
