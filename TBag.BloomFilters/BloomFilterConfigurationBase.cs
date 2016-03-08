using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBag.BloomFilters
{
    /// <summary>
    /// Base class for a Bloom filter configuration
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="THash"></typeparam>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="TIdHash"></typeparam>
    public abstract class BloomFilterConfigurationBase<T, THash, TId, TIdHash, TCount> :
        BloomFilterIdConfigurationBase<TId, TIdHash>,
        IBloomFilterConfiguration<T, THash, TId, TIdHash, TCount>
        where THash : struct
        where TIdHash : struct
        where TCount : struct
    {
        /// <summary>
        /// Get the hash for the values of an entity.
        /// </summary>
        public Func<T, THash> GetEntityHash { get; set; }

        /// <summary>
        /// Get the identifier for an entity.
        /// </summary>
        public Func<T, TId> GetId { get; set; }

        /// <summary>
        /// Function to determine if an entity hash equals the identity value (for example: zero for numbers).
        /// </summary>
        public Func<THash, bool> IsEntityHashIdentity { get; set; }

        /// <summary>
        /// Calculate the XOR of two entity hasehs.
        /// </summary>
        public Func<THash, THash, THash> EntityHashXor { get; set; }

        /// <summary>
        /// The unity for the count type.
        /// </summary>
        public Func<TCount> CountUnity { get; set; }

        /// <summary>
        /// Determine if the count is pure.
        /// </summary>
        public Func<TCount, bool> IsPureCount { get; set; }

        /// <summary>
        /// Increase the count.
        /// </summary>
        public Func<TCount,TCount> CountIncrease { get; set; }

        /// <summary>
        /// Decrease the count
        /// </summary>
        public Func<TCount,TCount> CountDecrease { get; set; }

        /// <summary>
        /// Count identity.
        /// </summary>
        public Func<TCount> CountIdentity { get; set; }

        /// <summary>
        /// Subtract two counts.
        /// </summary>
        public Func<TCount, TCount, TCount> CountSubtract { get; set; }

        public abstract bool Supports(ulong capacity, ulong size);
    }
}

  
