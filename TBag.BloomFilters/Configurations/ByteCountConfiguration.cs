namespace TBag.BloomFilters.Configurations
{
    using System;
    using System.Collections.Generic;
   
    /// <summary>
    /// Count configuration for type <see cref="sbyte"/>.
    /// </summary>
    public class ByteCountConfiguration : ICountConfiguration<sbyte>
    {
        /// <summary>
        /// Decrease the count
        /// </summary>
        public Func<sbyte, sbyte> Decrease { get; set; } =  sb => (sbyte)(sb - 1);

        /// <summary>
        /// Identity for the count (0).
        /// </summary>
        public Func<sbyte> Identity { get; set; } = ()=>0;

        /// <summary>
        /// Increase the count
        /// </summary>
        public Func<sbyte, sbyte> Increase { get; set; } = sb => (sbyte)(sb + 1);

        /// <summary>
        /// Subtract two count values
        /// </summary>
        public Func<sbyte, sbyte, sbyte> Subtract { get; set; } = (sb1,sb2) => (sbyte)(sb1 - sb2);

        /// <summary>
        /// Unity of the count (1).
        /// </summary>
        public Func<sbyte> Unity { get; set; } = ()=>1;

        /// <summary>
        /// Equality comparer for the count
        /// </summary>
        public IEqualityComparer<sbyte> EqualityComparer { get; set; } = EqualityComparer<sbyte>.Default;

        /// <summary>
        /// Determine if the count is pure.
        /// </summary>
        public Func<sbyte, bool> IsPure { get; set; } = sb => Math.Abs(sb) == 1;

        /// <summary>
        /// Add two counts.
        /// </summary>
        public Func<sbyte, sbyte, sbyte> Add { get; set; } = (sb1, sb2) => (sbyte)(sb1 + sb2);
    }
}
