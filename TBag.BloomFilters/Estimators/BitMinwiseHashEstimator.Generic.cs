namespace TBag.BloomFilters.Estimators
{
    using Configurations;
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
        private long _capacity;
        private Lazy<int[]> _slots ;
        private long _itemCount;
        private readonly IBloomFilterConfiguration<TEntity, TId, int, TCount> _configuration;
  

        #endregion

        #region Properties

        /// <summary>
        /// The number of items in the estimator.
        /// </summary>
        public virtual long ItemCount => _itemCount;

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
            long capacity)
        {
            _hashCount = hashCount;
            _configuration = configuration;
            _hashFunctions = GenerateHashes();
           _bitSize = bitSize;
            _capacity = _configuration.FoldingStrategy?.ComputeFoldableSize(capacity, 0) ?? capacity;
             _entityHash = e => unchecked((int)((ulong)(_configuration.EntityHash(e)+configuration.IdHash(_configuration.GetId(e)))));
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
        public double? Similarity(IBitMinwiseHashEstimator<TEntity, TId, TCount> estimator)
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
        public double? Similarity(IBitMinwiseHashEstimatorData estimatorData)
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
        /// Add an estimator
        /// </summary>
        /// <param name="estimator">The estimator to add.</param>
        /// <returns></returns>
        public void Add(IBitMinwiseHashEstimator<TEntity, TId, TCount> estimator)
        {
            Rehydrate(FullExtract().Add(estimator?.FullExtract(), _configuration.FoldingStrategy, true));
        }

        /// <summary>
        /// Add an estimator
        /// </summary>
        /// <param name="estimator">The estimator to add.</param>
        /// <returns></returns>
        public void Add(IBitMinwiseHashEstimatorFullData estimator)
        {
            Rehydrate(FullExtract().Add(estimator, _configuration.FoldingStrategy, true));
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
                Values = !_slots.IsValueCreated ? null : _slots.Value.ConvertToBitArray(_bitSize).ToBytes(),
                ItemCount = _itemCount
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
                Values = !_slots.IsValueCreated ? null : _slots.Value,
                ItemCount = _itemCount
            };
        }

        /// <summary>
        /// Compress the estimator.
        /// </summary>
        /// <param name="inPlace"></param>
        /// <returns></returns>
        public IBitMinwiseHashEstimator<TEntity, TId, TCount> Compress(bool inPlace = false)
        {
            var res = FullExtract().Compress(_configuration);
            if (inPlace)
            {
                Rehydrate(res);
                return this;
            }
            var estimatorRes = new BitMinwiseHashEstimator<TEntity, TId, TCount>(_configuration, _bitSize, _hashCount, _capacity);
            estimatorRes.Rehydrate(res);
            return estimatorRes;
        }

        /// <summary>
        /// Fold the estimator.
        /// </summary>
        /// <param name="factor">Factor to fold by.</param>
        /// <param name="inPlace">When <c>true</c> the estimator will be replaced by a folded estimator, else <c>false</c>.</param>
        /// <returns></returns>
        public BitMinwiseHashEstimator<TEntity, TId, TCount> Fold(uint factor, bool inPlace = false)
        {
            var res = FullExtract().Fold(factor);
            if (inPlace)
            {
                Rehydrate(res);
                return this;
            }
            var estimator = new BitMinwiseHashEstimator<TEntity, TId, TCount>(
                _configuration, 
                res.BitSize, 
                res.HashCount,
                res.Capacity);
            estimator.Rehydrate(res);
            return estimator;
        }

        /// <summary>
        /// Explicit implementation of the interface for folding.
        /// </summary>
        /// <param name="factor"></param>
        /// <param name="inPlace"></param>
        /// <returns></returns>
        IBitMinwiseHashEstimator<TEntity, TId, TCount> IBitMinwiseHashEstimator<TEntity, TId, TCount>.Fold(uint factor, bool inPlace)
        {
            return Fold(factor, inPlace);
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
            _itemCount = data.ItemCount;
            _slots = data.Values == null
                ? new Lazy<int[]>(() => GetMinHashSlots(_hashCount, _capacity))
                : new Lazy<int[]>(() => data.Values);
        }        

        #endregion

        #region Methods

      

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
            var idx = 0L;
            var idhash = Math.Abs(unchecked(entityHash % _capacity));
            for (var i = 0L; i < entityHashes.LongLength; i++)
            {
                 if (entityHashes[i] < _slots.Value[idx+idhash])
                {
                    _slots.Value[idx + idhash] = entityHashes[i];
                }
                idx += _capacity;
            }
            _itemCount++;
        }

        /// <summary>
        /// Create an array of the correct size.
        /// </summary>
        /// <param name="numHashFunctions"></param>
        /// <param name="setSize"></param>
        /// <returns></returns>
        private static int[] GetMinHashSlots(int numHashFunctions, long setSize)
        {
            var minHashValues = new int[numHashFunctions*setSize];
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
