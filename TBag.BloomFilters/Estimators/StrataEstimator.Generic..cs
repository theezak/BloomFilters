namespace TBag.BloomFilters.Estimators
{
    using Configurations;
    using HashAlgorithms;
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
        private  long _capacity;
         protected const byte MaxTrailingZeros = sizeof(int)*8;
        private readonly IMurmurHash _murmur = new Murmur3();
        #endregion

        #region Properties

        /// <summary>
        /// The maximum strata.
        /// </summary>
        protected byte MaxStrata { get; set; } = MaxTrailingZeros;     

        /// <summary>
        /// The item count.
        /// </summary>
        public virtual long ItemCount
            => (StrataFilters?.Sum(filter => (filter?.IsValueCreated ?? false) ? filter.Value.ItemCount : 0L) ?? 0L);

        /// <summary>
        /// Strata filters.
        /// </summary>
        protected Lazy<InvertibleBloomFilter<KeyValuePair<int,int>, int, TCount>>[] StrataFilters { get; } =
           new Lazy<InvertibleBloomFilter<KeyValuePair<int, int>, int, TCount>>[MaxTrailingZeros];

        /// <summary>
        /// Configuration
        /// </summary>
        protected IBloomFilterConfiguration<TEntity, TId,  int, TCount> Configuration { get; }

        /// <summary>
        /// Decode factor.
        /// </summary>
        public double DecodeCountFactor { get; set; }
        #endregion

        #region Constructor       
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="capacity">The capacity (size of the set to be added)</param>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <param name="maxStrata">Optional maximum strata</param>
             protected StrataEstimator(
            long capacity,
            IBloomFilterConfiguration<TEntity, TId,  int, TCount> configuration,
            byte? maxStrata = null)
        {
            _capacity = capacity;
            if (maxStrata.HasValue)
            {
                if (maxStrata <= 0 || maxStrata > MaxTrailingZeros)
                {
                    throw new ArgumentOutOfRangeException(nameof(maxStrata), $"Maximimum strata value {maxStrata.Value} is not in the valid range [1, {MaxTrailingZeros}].");
                }
                MaxStrata = maxStrata.Value;
            }
            Configuration = configuration;
            DecodeCountFactor = _capacity >= 20 ? 1.39D : 1.0D;
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
                Capacity = _capacity,
                DecodeCountFactor = DecodeCountFactor,
                StrataCount =  MaxStrata,
                BloomFilters = StrataFilters.Where(s=>s?.IsValueCreated??false).Select(s => s.Value.Extract()).ToArray(),
                BloomFilterStrataIndexes = StrataFilters.Select((s,i) => new { Index = (byte)i, Include = s?.IsValueCreated??false }).Where(i=>i.Include).Select(i=>i.Index).ToArray()
            };
        }

        /// <summary>
        /// Restore the strata estimator from the given data
        /// </summary>
        /// <param name="data"></param>
        public void Rehydrate(IStrataEstimatorData<int, TCount> data)
        {
            if (data == null) return;
            _capacity = data.Capacity;
            MaxStrata = data.StrataCount;
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
            var strataEstimator = new StrataEstimator<TEntity, TId, TCount>(res.Capacity, Configuration, res.StrataCount);
            strataEstimator.Rehydrate(res);
            return strataEstimator;
        }

        /// <summary>
        /// Decode the given estimator data.
        /// </summary>
        /// <param name="estimator">Estimator data to subtract.</param>
        /// <param name="destructive">When <c>true</c> the values in this estimator will be altered and rendered useless, else <c>false</c>.</param>
        /// <returns></returns>
        public virtual long? Decode(IStrataEstimatorData<int, TCount> estimator,
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
            return Decode(estimator.Extract(), destructive);
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
            var estimator = new StrataEstimator<TEntity, TId, TCount>(res.Capacity, Configuration, res.StrataCount);
            estimator.Rehydrate(res);
            return estimator;

        }
        #endregion

        #region Methods
        /// <summary>
        /// Determine the number of trailing zeros in the bit representation of <paramref name="idHash"/>.
        /// </summary>
        /// <param name="idHash">The idHash</param>
        /// <param name="entityHash">The entity hash</param>
        /// <returns>number of trailing zeros. Determines the strata to add an item to.</returns>
        protected int GetStrata(int idHash, int entityHash)
        {
            idHash = BitConverter.ToInt32(_murmur.Hash(BitConverter.GetBytes(idHash), unchecked((uint)entityHash)), 0);
            var mask = 1;
            for (var i = 0; i < MaxTrailingZeros; i++, mask <<= 1)
                if ((idHash & mask) != 0)
                    return i;
            return MaxTrailingZeros;
        }

       /// <summary>
       /// Remove a given key and value from the position
       /// </summary>
       /// <param name="key">The key</param>
       /// <param name="value">The value</param>
       /// <param name="idx">The position</param>
        protected void Remove(int key, int value, long idx)
        {
            StrataFilters[idx]?.Value.Remove(new KeyValuePair<int, int>(key, value));
        }

     /// <summary>
     /// Add a given key and value to the provided position
     /// </summary>
     /// <param name="key">The key</param>
     /// <param name="valueHash">The value</param>
     /// <param name="idx">The position</param>
        protected void Add(int key, int valueHash, long idx)
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
                    res.Initialize(_capacity, 0.001F);
                     res.Rehydrate(filterData);
                    return res;
                });
            }
        }

        #endregion
    }
}
