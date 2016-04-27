namespace TBag.BloomFilters.Invertible.Estimators
{
    using Configurations;
    using HashAlgorithms;
    using Invertible;
     using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The strata estimator helps estimate the number of differences between two (sub)sets.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TCount">The type of occurence count.</typeparam>
    /// <typeparam name="TId">The entity identifier type</typeparam>
    /// <remarks>For higher strata's, MinWise gives a better accuracy/size balance. Also recognizes that for key/value pairs, the estimator should utilize the full entity hash (which includes identifier and entity value), rather than just the identifier hash.</remarks>
    public class StrataEstimator<TEntity, TId, TCount> : IStrataEstimator<TEntity, int, TCount>
        where TCount : struct
        where TId : struct
    {
        #region Fields
       private const byte MaxTrailingZeros = sizeof(int) * 8;
        private readonly IMurmurHash _murmur = new Murmur3();
        private const float ErrorRate = 0.001F;
        #endregion

        #region Properties
        /// <summary>
        /// Limit on the strata
        /// </summary>
        public byte StrataLimit = MaxTrailingZeros;

        /// <summary>
        /// The maximum strata.
        /// </summary>
        public byte MaxStrata { get; protected set; } = MaxTrailingZeros;   
        
        /// <summary>
        /// The block size
        /// </summary>
        public long BlockSize
        {
            get; protected set;
        }

        /// <summary>
        /// The hash function count
        /// </summary>
        public uint HashFunctionCount { get; private set; }

        /// <summary>
        /// The item count.
        /// </summary>
        public virtual long ItemCount
            => StrataFilters?
                .Sum(filter => (filter?.IsValueCreated ?? false)
                    ? filter.Value.ItemCount
                    : 0L) ?? 0L;

        /// <summary>
        /// Strata filters.
        /// </summary>
        protected Lazy<InvertibleBloomFilter<KeyValuePair<int,int>, int, TCount>>[] StrataFilters { get; } =
           new Lazy<InvertibleBloomFilter<KeyValuePair<int, int>, int, TCount>>[MaxTrailingZeros];

        /// <summary>
        /// Configuration
        /// </summary>
        protected IInvertibleBloomFilterConfiguration<TEntity, TId,  int, TCount> Configuration { get; }

        /// <summary>
        /// Decode factor.
        /// </summary>
        public double DecodeCountFactor { get; set; }
        #endregion

        #region Constructor       
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="blockSize">The capacity (size of the set to be added)</param>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <param name="maxStrata">Optional maximum strata</param>
            public StrataEstimator(
            long blockSize,
            IInvertibleBloomFilterConfiguration<TEntity, TId,  int, TCount> configuration,
            byte? maxStrata = null,
            bool fixedBlockSize = false)
        {
            BlockSize = fixedBlockSize ? 
                blockSize :
                configuration.FoldingStrategy?.ComputeFoldableSize(blockSize, 0) ?? blockSize;
            if (maxStrata.HasValue)
            {
                if (maxStrata <= 0 || maxStrata > MaxTrailingZeros)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(maxStrata), 
                        $"Maximimum strata value {maxStrata.Value} is not in the valid range [1, {MaxTrailingZeros}].");
                }
                MaxStrata = maxStrata.Value;
            }
            Configuration = configuration;
            DecodeCountFactor = BlockSize >= 20 ? 1.39D : 1.0D;
             CreateFilters();
        }
        #endregion

        #region Implementation of IStrataEstimator{TEntity, TId, TCount}
        /// <summary>
        /// Extract the data
        /// </summary>
        /// <returns></returns>
        public StrataEstimatorData<int, TCount> Extract()
        {
            return new StrataEstimatorData<int, TCount>
            {
                BlockSize = BlockSize,
                DecodeCountFactor = DecodeCountFactor,
                StrataCount = MaxStrata,
                HashFunctionCount =  HashFunctionCount,
                BloomFilters = StrataFilters
                    .Where(s => s?.IsValueCreated ?? false)
                    .Select(s => s.Value.Extract())
                    .ToArray(),
                BloomFilterStrataIndexes = StrataFilters
                    .Select((s, i) => new {Index = (byte) i, Include = s?.IsValueCreated ?? false})
                    .Where(i => i.Include)
                    .Select(i => i.Index)
                    .ToArray()
            };
        }

        /// <summary>
        /// Restore the strata estimator from the given data
        /// </summary>
        /// <param name="data"></param>
        public void Rehydrate(IStrataEstimatorData<int, TCount> data)
        {
            if (data == null) return;
            BlockSize = data.BlockSize;
            MaxStrata = data.StrataCount;
            HashFunctionCount = data.HashFunctionCount;
            DecodeCountFactor = data.DecodeCountFactor;            
            CreateFilters(data);
        }

        /// <summary>
        /// Add an item to strata estimator.
        /// </summary>
        /// <param name="item">Item to add</param>
        public virtual void Add(TEntity item)
        {
            var idHash = Configuration.IdHash(Configuration.GetId(item));
            var entityHash = Configuration.EntityHash(item);
            Add(idHash, entityHash, GetStrata(idHash, entityHash));
        }

        /// <summary>
        /// Remove an item from the strata estimator.
        /// </summary>
        /// <param name="item">Item to remove</param>
        public virtual void Remove(TEntity item)
        {
            var idHash = Configuration.IdHash(Configuration.GetId(item));
            var entityHash = Configuration.EntityHash(item);
            Remove(idHash, entityHash, GetStrata(idHash, entityHash));
        }

        /// <summary>
        /// Determine if an item is in the estimator
        /// </summary>
        /// <param name="item"></param>
        /// <returns><c>null</c> when the strata estimator can't determine membership (strata for the item is above the maximum strata), otherwise <c>true</c> when a member, or <c>false</c> when not a member.</returns>
        public bool? Contains(TEntity item)
        {
            var idHash = Configuration.IdHash(Configuration.GetId(item));
            var entityHash = Configuration.EntityHash(item);
            var strata = GetStrata(idHash, entityHash);
            if (strata >= MaxStrata) return null;
            return StrataFilters[strata].Value.Contains(new KeyValuePair<int, int>(idHash, entityHash));
        }

        /// <summary>
        /// Fold the strata estimator.
        /// </summary>
        /// <param name="factor"></param>
        /// <param name="inPlace"></param>
        /// <returns></returns>
        public virtual IStrataEstimator<TEntity, int, TCount> Fold(uint factor, bool inPlace = false)
        {
            var res = Extract().Fold(Configuration, factor);
            if (inPlace)
            {
                Rehydrate(res);
                return this;
            }
            var strataEstimator = new StrataEstimator<TEntity, TId, TCount>(res.BlockSize, Configuration, res.StrataCount);
            strataEstimator.Rehydrate(res);
            return strataEstimator;
        }

        /// <summary>
        /// Decode the given estimator data.
        /// </summary>
        /// <param name="estimator">Estimator data to subtract.</param>
        /// <param name="destructive">When <c>true</c> the values in this estimator will be altered and rendered useless, else <c>false</c>.</param>
        /// <returns></returns>
        private long? Decode(IStrataEstimatorData<int, TCount> estimator,
            bool destructive = false)
        {
            return Extract()
                .Decode(estimator, Configuration, MaxStrata, destructive);
        }

        /// <summary>
        /// Decode
        /// </summary>
        /// <param name="estimator">Other estimator.</param>
        /// <param name="destructive">When <c>true</c> the values in this estimator will be altered and rendered useless, else <c>false</c>.</param>
        /// <returns></returns>
        public virtual long? Decode(IStrataEstimator<TEntity, int, TCount> estimator,
            bool destructive = false)
        {
            return Decode(estimator?.Extract(), destructive);
        }

        /// <summary>
        /// Intersect the given estimator data.
        /// </summary>
        /// <param name="estimator">Estimator data to intersect with.</param>
        /// <returns></returns>
        private void Intersect(IStrataEstimatorData<int, TCount> estimator,
            bool destructive = false)
        {
            Rehydrate(Extract()
                .Intersect(estimator, Configuration));
        }

        /// <summary>
        /// Intersect with the given estimator
        /// </summary>
        /// <param name="estimator">Other estimator.</param>
        /// <returns></returns>
        public virtual void Intersect(IStrataEstimator<TEntity, int, TCount> estimator)
        {
            Intersect(estimator?.Extract());
        }

        /// <summary>
        /// Compress the estimator.
        /// </summary>
        /// <param name="inPlace"></param>
        /// <returns></returns>
        public IStrataEstimator<TEntity, int, TCount> Compress(bool inPlace = false)
        {
            var res = Extract().Compress(Configuration);
            if (inPlace)
            {
                Rehydrate(res);
                return this;
            }
            var estimator = new StrataEstimator<TEntity, TId, TCount>(res.BlockSize, Configuration, res.StrataCount);
            estimator.Rehydrate(res);
            return estimator;

        }

        /// <summary>
        ///Only adds the identifier and entity hash to the estimator when the number of trailing zeros of their hash falls within the strata range.
        /// </summary>
        /// <param name="idHash"></param>
        /// <param name="entityHash"></param>
        /// <returns>Returns <c>true</c> when added, else false.</returns>
        public bool ConditionalAdd(int idHash, int entityHash)
        {
            var strata = GetStrata(idHash, entityHash);
            if (strata < MaxStrata)
            {
                Add(idHash, entityHash, strata);
                return true;
            }
            return false;
        }

        /// <summary>
        ///Only removes the identifier and entity hash to the estimator when the number of trailing zeros of their hash falls within the strata range.
        /// </summary>
        /// <param name="idHash"></param>
        /// <param name="entityHash"></param>
        /// <returns>Returns <c>true</c> when removed, else false.</returns>
        public bool ConditionalRemove(int idHash, int entityHash)
        {
            var strata = GetStrata(idHash, entityHash);
            if (strata < MaxStrata)
            {
                Remove(idHash, entityHash, strata);
                return true;
            }
            return false;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Determine the number of trailing zeros in the bit representation of <paramref name="idHash"/>.
        /// </summary>
        /// <param name="idHash">The idHash</param>
        /// <param name="entityHash">The entity hash</param>
        /// <returns>number of trailing zeros. Determines the strata to add an item to.</returns>
        private byte GetStrata(int idHash, int entityHash)
        {
            idHash = BitConverter.ToInt32(_murmur.Hash(BitConverter.GetBytes(idHash), unchecked((uint)entityHash)), 0);
            if (idHash==0) return StrataLimit;           
            int mask = 1;
            for (byte i = 0; i < StrataLimit; i++, mask <<= 1)
                if ((idHash & mask) != 0)
                    return i;
            return StrataLimit;
        }

       /// <summary>
       /// Remove a given key and value from the position
       /// </summary>
       /// <param name="key">The key</param>
       /// <param name="value">The value</param>
       /// <param name="idx">The position</param>
        internal void Remove(int key, int value, long idx)
        {
            StrataFilters[idx]?.Value.Remove(new KeyValuePair<int, int>(key, value));
        }

     /// <summary>
     /// Add a given key and value to the provided position
     /// </summary>
     /// <param name="key">The key</param>
     /// <param name="valueHash">The value</param>
     /// <param name="idx">The position</param>
        internal void Add(int key, int valueHash, long idx)
        {        
            StrataFilters[idx]?.Value.Add(new KeyValuePair<int, int>(key, valueHash));
        }

        /// <summary>
        /// Create filters
        /// </summary>
        /// <param name="estimatorData">Filter data to rehydrate.</param>
        private void CreateFilters(IStrataEstimatorData<int, TCount> estimatorData = null)
        {
            var configuration = Configuration.ConvertToEstimatorConfiguration();
            HashFunctionCount = configuration.BestHashFunctionCount(BlockSize, ErrorRate);
            for (var idx = 0; idx < StrataFilters.Length; idx++)
            {
                if (idx >= MaxStrata)
                {
                    StrataFilters[idx] = null;
                    continue;
                }
                var filterData = estimatorData.GetFilterForStrata(idx);
                //lazily create Strata filters.
                StrataFilters[idx] = new Lazy<InvertibleBloomFilter<KeyValuePair<int, int>, int, TCount>>(() =>
                {
                    var res = new InvertibleBloomFilter<KeyValuePair<int, int>, int, TCount>(configuration);
                    //capacity doesn't really matter, the capacity is basically the block size.
                    res.Initialize(BlockSize, BlockSize, HashFunctionCount);
                    res.Rehydrate(filterData);
                    return res;
                });
            }
        }
        #endregion
    }
}
