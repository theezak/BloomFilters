namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
  
    /// <summary>
    /// Base class for the Bloom filter configuration for identifiers.
    /// </summary>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="THash"></typeparam>
    public abstract class BloomFilterIdConfigurationBase<TId, THash>
       where THash : struct
    {
        /// <summary>
        /// Function to determine a sequence (of given length) for a given identifier.
        /// </summary>
        public virtual Func<TId, uint, IEnumerable<THash>> IdHashes { get; set; }

        /// <summary>
        /// Determine the XOR of two identifiers.
        /// </summary>
        public virtual Func<TId, TId, TId> IdXor { get; set; }

        /// <summary>
        /// Determine if the identifier equals the identity value (for example: zero for numbers)
        /// </summary>
        public virtual Func<TId, bool> IsIdIdentity { get; set; }
    }
}