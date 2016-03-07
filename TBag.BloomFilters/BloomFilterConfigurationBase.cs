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
    public abstract class BloomFilterConfigurationBase<T, THash, TId, TIdHash> :
        BloomFilterIdConfigurationBase<TId, TIdHash>,
        IBloomFilterConfiguration<T, THash, TId, TIdHash>
        where THash : struct
        where TIdHash : struct
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
    }
}

  
