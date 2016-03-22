
using System.Diagnostics.Contracts;

namespace TBag.BloomFilters.Estimators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The strata estimator helps estimate the number of differences between two (sub)sets.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TCount">The type of occurence count.</typeparam>
    /// <remarks>For higher strata's, MinWise gives a better accuracy/size balance. Also recognizes that for key/value pairs, the estimator should utilize the full entity hash (which includes identifier and entity value), rather than just the identifier hash.</remarks>
    public class StrataEstimator<TEntity, TId, TCount> : IStrataEstimator<TEntity, int, TCount>
        where TCount : struct
        where TId : struct
    {
        #region Fields
        private  long _capacity;
         protected const byte MaxTrailingZeros = sizeof(int)*8;
        #endregion

        #region Properties
        /// <summary>
        /// The maximum strata.
        /// </summary>
        protected byte MaxStrata { get; set; }

        /// <summary>
        /// Strata filters.
        /// </summary>
        protected InvertibleBloomFilter<KeyValuePair<int,int>, int, TCount>[] StrataFilters { get; } =
           new InvertibleBloomFilter<KeyValuePair<int, int>, int, TCount>[MaxTrailingZeros];

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
        /// <param name="capacity"></param>
        /// <param name="configuration"></param>
        public StrataEstimator(
            long capacity, 
            IBloomFilterConfiguration<TEntity,TId,int,TCount> configuration) : this(capacity, configuration, MaxTrailingZeros)
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="configuration"></param>
        /// <param name="maxStrata"></param>
             protected StrataEstimator(
            long capacity,
            IBloomFilterConfiguration<TEntity, TId,  int, TCount> configuration,
            byte maxStrata)
        {
            _capacity = capacity;
            MaxStrata = maxStrata;
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
                BloomFilters = StrataFilters.Select(s => s?.Extract()).ToArray()
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
            DecodeCountFactor = data.DecodeCountFactor;
            if (data.BloomFilters == null) return;
            for (int i = 0; i < StrataFilters.Length; i++)
            {
                if (data.BloomFilters.Length > i &&
                    data.BloomFilters[i] == null)
                {
                    StrataFilters[i]?.Rehydrate(data.BloomFilters[i]);
                }
            }
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
        /// Decode the given estimator data.
        /// </summary>
        /// <param name="estimator">Estimator data to subtract.</param>
        /// <param name="destructive">When <c>true</c> the values in this estimator will be altered and rendered useless, else <c>false</c>.</param>
        /// <returns></returns>
        public virtual long? Decode(IStrataEstimatorData<int, TCount> estimator,
            bool destructive = false)
        {
            return Extract()
                .Decode(estimator, Configuration, destructive);
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
        #endregion

        #region Methods
        /// <summary>
        /// Determine the number of trailing zeros in the bit representation of <paramref name="idHash"/>.
        /// </summary>
        /// <param name="idHash">The idHash</param>
        /// <param name="entityHash">The entity hash</param>
        /// <returns>number of trailing zeros. Determines the strata to add an item to.</returns>
        protected static int GetStrata(int idHash, int entityHash)
        {
            idHash = unchecked((int)(idHash + 3 * entityHash));
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
            StrataFilters[idx]?.Remove(new KeyValuePair<int, int>(key, value));
        }

     /// <summary>
     /// Add a given key and value to the provided position
     /// </summary>
     /// <param name="key">The key</param>
     /// <param name="valueHash">The value</param>
     /// <param name="idx">The position</param>
        protected void Add(int key, int valueHash, long idx)
        {
            StrataFilters[idx]?.Add(new KeyValuePair<int, int>(key, valueHash));
        }

        /// <summary>
        /// Create new filters
        /// </summary>
        private void CreateFilters()
        {
            for (var idx = 0; idx < StrataFilters.Length; idx++)
            {
                if (idx >= MaxStrata)
                {
                    StrataFilters[idx] = null;
                    continue;
                }
                StrataFilters[idx] = new InvertibleBloomFilter<KeyValuePair<int,int>, int, TCount>(
                    Configuration.ConvertToEstimatorConfiguration());
                StrataFilters[idx].Initialize(_capacity, 0.001F);
            }
        }
        #endregion
    }
}
