using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBag.BloomFilters
{
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
        public Func<TId, uint, IEnumerable<THash>> IdHashes { get; set; }

        /// <summary>
        /// Determine the XOR of two identifiers.
        /// </summary>
        public Func<TId, TId, TId> IdXor { get; set; }

        /// <summary>
        /// Determine if the identifier equals the identity value (for example: zero for numbers)
        /// </summary>
        public Func<TId, bool> IsIdIdentity { get; set; }

        /// <summary>
        /// When <c>true</c> then hash values are split by the hash function used.
        /// </summary>
        public bool SplitByHash
        {
            get; set;
        }
    }
}