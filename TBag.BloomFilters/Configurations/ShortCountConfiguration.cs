namespace TBag.BloomFilters.Configurations
{
    using System;
    using System.Collections.Generic;
   
    /// <summary>
    /// Count configuration for type <see cref="short"/>.
    /// </summary>
    public class ShortCountConfiguration : ICountConfiguration<short>
    {
        /// <summary>
        /// Decrease the count
        /// </summary>
        public Func<short, short> Decrease { get; set; } =  sb => (short)(sb - 1);

        /// <summary>
        /// Identity for the count (0).
        /// </summary>
        public Func<short> Identity { get; set; } = ()=>0;

        /// <summary>
        /// Increase the count
        /// </summary>
        public Func<short, short> Increase { get; set; } = sb => (short)(sb + 1);

        /// <summary>
        /// Subtract two count values
        /// </summary>
        public Func<short, short, short> Subtract { get; set; } = (sb1,sb2) => (short)(sb1 - sb2);

        /// <summary>
        /// Unity of the count (1).
        /// </summary>
        public Func<short> Unity { get; set; } = ()=>1;

        /// <summary>
        /// Equality comparer for the count
        /// </summary>
        public IEqualityComparer<short> EqualityComparer { get; set; } = EqualityComparer<short>.Default;

        /// <summary>
        /// Determine if the count is pure.
        /// </summary>
        public Func<short, bool> IsPure { get; set; } = sb => Math.Abs(sb) == 1;

        /// <summary>
        /// Add two counts
        /// </summary>
        public Func<short, short, short> Add { get; set; } = (c1, c2) => (short)(c1 + c2);
    }
}
