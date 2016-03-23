namespace TBag.BloomFilters.Estimators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// A b-bits min hash estimator.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TId">The entity identifier type</typeparam>
    /// <typeparam name="TCount">The type of occurence count.</typeparam>
    public class BitMinwiseHashEstimator<TEntity, TId, TCount> : IBitMinwiseHashEstimator<TEntity, TId, TCount> 
        where TCount : struct
        where TId : struct
    {
        #region Fields

        private int _hashCount;
        private readonly Func<int, IEnumerable<int>> _hashFunctions;
        private readonly Func<TEntity, int> _entityHash;
        private byte _bitSize;
        private ulong _capacity;
        private Lazy<int[]> _slots ;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="bitSize">The number of bits to store per hash</param>
        /// <param name="hashCount">The number of hash functions to use.</param>
        /// <param name="capacity">The capacity (should be a close approximation of the number of elements added)</param>
        /// <remarks>By using bitSize = 1 or bitSize = 2, the accuracy is decreased, thus the hashCount needs to be increased. However, when resemblance is not too small, for example > 0.5, bitSize = 1 can yield similar results as bitSize = 64 with only 3 times the hash count.</remarks>
        public BitMinwiseHashEstimator(
            IBloomFilterConfiguration<TEntity, TId,  int, TCount> configuration,
            byte bitSize,
            int hashCount,
            ulong capacity)
        {
            _bitSize = bitSize;
            _capacity = capacity;
            _hashCount = hashCount;
            _hashFunctions = GenerateHashes();
            _entityHash = e => unchecked((int)((ulong)(configuration.EntityHash(e)+configuration.IdHash(configuration.GetId(e)))));
            _slots = new Lazy<int[]>(()=> GetMinHashSlots(_hashCount, _capacity));
        }

        #endregion

        #region Implementation of estimator
        /// <summary>
        /// Determine similarity.
        /// </summary>
        /// <param name="estimator">The estimator to compare against.</param>
        /// <returns>Similarity (percentage similar, zero is completely different, one is completely the same)</returns>
        /// <remarks>Zero is no similarity, one is completely similar.</remarks>
        public double Similarity(IBitMinwiseHashEstimator<TEntity, TId, TCount> estimator)
        {
            if (estimator == null) return 0.0D;
            return Extract()
                .Similarity(estimator.Extract());
        }

        /// <summary>
        /// Determine the similarity between this estimator and the provided estimator data,
        /// </summary>
        /// <param name="estimatorData">The estimator data to compare against.</param>
        /// <returns></returns>
        public double Similarity(IBitMinwiseHashEstimatorData estimatorData)
        {
            if (estimatorData == null) return 0.0D;
            return Extract()
                .Similarity(estimatorData);
        }

        /// <summary>
        /// Add the item to estimator.
        /// </summary>
        /// <param name="item">The entity to add</param>
        public void Add(TEntity item)
        {
            Debug.Assert(item != null);
            ComputeMinHash(item);
        }

        /// <summary>
        /// Extract the estimator data in a serializable format.
        /// </summary>
        /// <returns></returns>
        public BitMinwiseHashEstimatorData Extract()
        {
            return new BitMinwiseHashEstimatorData
            {
                BitSize = _bitSize,
                Capacity = _capacity,
                HashCount = _hashCount,
                Values = Convert(_slots, _bitSize).ToBytes()
            };
        }

        /// <summary>
        /// Extract the full data from the b-bit inwise estimator
        /// </summary>
        /// <returns></returns>
        /// <remarks>Not for sending across the wire, but good for rehydrating.</remarks>
        public BitMinwiseHashEstimatorFullData FullExtract()
        {
            return new BitMinwiseHashEstimatorFullData
            {
                BitSize = _bitSize,
                Capacity = _capacity,
                HashCount = _hashCount,
                Values = !_slots.IsValueCreated ? null : _slots.Value
            };
        }

        /// <summary>
        /// Rehydrate the given data.
        /// </summary>
        /// <param name="data"></param>
        public void Rehydrate(IBitMinwiseHashEstimatorFullData data)
        {
            if (data == null) return;
            _capacity = data.Capacity;
            _hashCount = data.HashCount;
            _bitSize = data.BitSize;
            _slots = data.Values == null
                ? new Lazy<int[]>(() => GetMinHashSlots(_hashCount, _capacity))
                : new Lazy<int[]>(() => data.Values);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Convert the slots to a bit array that only includes the specificied number of bits per slot.
        /// </summary>
        /// <param name="slots">The hashed values.</param>
        /// <param name="bitSize">The bit size to be used per slot.</param>
        /// <returns></returns>
        private static BitArray Convert(Lazy<int[]> slots, byte bitSize)
        {
            if (!slots.IsValueCreated || bitSize <= 0) return null;
            var hashValues = new BitArray((int)(bitSize * slots.Value.LongLength));
            var allDefault = true;
            var idx = 0;
            for (var i = 0; i < slots.Value.LongLength; i++)
            {
                allDefault = allDefault && slots.Value[i] == int.MaxValue;
                var byteValue = BitConverter.GetBytes(slots.Value[i]);
                var byteValueIdx = 0;
                for (var b = 0; b < bitSize; b++)
                {
                    hashValues.Set(idx + b, (byteValue[byteValueIdx] & (1 << (b % 8))) != 0);
                    if (b > 0 && b % 8 == 0)
                    {
                        byteValueIdx = (byteValueIdx + 1) % byteValue.Length;
                    }
                }
                idx += bitSize;
            }
            if (allDefault) return null;
            return hashValues;
        }

        /// <summary>
        /// Bit minwise estimator requires this specific hash function.
        /// </summary>
        /// <returns></returns>
        private Func<int, IEnumerable<int>> GenerateHashes()
        {
            const int universeSize = int.MaxValue;
            var bound = (uint)universeSize;
            var r = new Random(11);
            var hashFuncs = new Func<int, int>[_hashCount];
            for (var i = 0; i < _hashCount; i++)
            {
                var a = unchecked((uint)r.Next(universeSize));
                var b = unchecked((uint)r.Next(universeSize));
                var c = unchecked((uint)r.Next(universeSize));
                hashFuncs[i] = hash => QHash(hash, a, b, c, bound);
            }
            return hash => hashFuncs.Select(f => f(hash));
        }

        /// <summary>
        /// Compute the hash for the given element.
        /// </summary>
        /// <param name="element"></param>
        private void ComputeMinHash(TEntity element)
        {
            var entityHash =_entityHash(element);
            var entityHashes = _hashFunctions(entityHash).ToArray();
            ulong idx = 0;
            var idhash = (ulong)Math.Abs(unchecked((long)((ulong)entityHash % _capacity)));
            for (var i = 0L; i < entityHashes.LongLength; i++)
            {
                 if (entityHashes[i] < _slots.Value[idx+idhash])
                {
                    _slots.Value[idx + idhash] = entityHashes[i];
                }
                idx += _capacity;
            }
        }

        /// <summary>
        /// Create an array of the correct size.
        /// </summary>
        /// <param name="numHashFunctions"></param>
        /// <param name="setSize"></param>
        /// <returns></returns>
        private static int[] GetMinHashSlots(int numHashFunctions, ulong setSize)
        {
            var minHashValues = new int[(ulong)numHashFunctions*setSize];
            for (var i = 0L; i < minHashValues.LongLength; i++)
            {
                minHashValues[i] = int.MaxValue;
            }
            return minHashValues;
        }

        /// <summary>
        /// QHash.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="bound"></param>
        /// <returns></returns>
        private static int QHash(int id, uint a, uint b, uint c, uint bound)
        {
            //Modify the hash family as per the size of possible elements in a Set
            return unchecked((int) (Math.Abs((int) ((a*(id >> 4) + b*id + c) & 131071))%bound));
        }
        #endregion
    }
}
