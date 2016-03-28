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
        public Func<int, int> Decrease { get; set; } = i => i- 1;

        /// <summary>
        /// The count identity value (0)
        /// </summary>
        public Func<int> Identity { get; set; } = ()=>0;

        /// <summary>
        /// Increase the count
        /// </summary>
        public Func<int, int> Increase { get; set; } = i => i + 1;

        /// <summary>
        /// Subtract counts
        /// </summary>
        public Func<int, int, int> Subtract { get; set; } = (i1,i2)=>i1-i2;

        /// <summary>
        /// The count unity (1)
        /// </summary>
        public Func<int> Unity { get; set; } = ()=>1;

        /// <summary>
        /// Count equality comparer
        /// </summary>
        public IEqualityComparer<int> EqualityComparer { get; set; } = EqualityComparer<int>.Default;

        /// <summary>
        /// Determine if the count is pure.
        /// </summary>
        public Func<int, bool> IsPure { get; set; } = i => Math.Abs(i) == 1;

        /// <summary>
        /// Add two counts
        /// </summary>
        public Func<int, int, int> Add { get; set; } = (c1, c2) => c1 + c2;
    }
}
