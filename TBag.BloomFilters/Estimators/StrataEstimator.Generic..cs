
namespace TBag.BloomFilters.Estimators
{
    using System;
     using System.Linq;

    /// <summary>
    /// The strata estimator helps estimate the number of differences between two (sub)sets.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
     /// <typeparam name="TCount">The type of occurence count.</typeparam>
    /// <remarks>For higher strata's, MinWise gives a better accuracy/size balance. Also recognizes that for key/value pairs, the estimator should utilize the full entity hash (which includes identifier and entity value), rather than just the identifier hash.</remarks>
    public class StrataEstimator<TEntity, TCount> : IStrataEstimator<TEntity, int, TCount>
        where TCount : struct
    {
        #region Fields
        private readonly long _capacity;       
         protected const byte MaxTrailingZeros = sizeof(long)*8;
        #endregion

        #region Properties
        /// <summary>
        /// Function to determine the hash value for a given entity.
        /// </summary>
        protected Func<TEntity, int> EntityHash { get;  }

        /// <summary>
        /// Strata filters.
        /// </summary>
        protected InvertibleBloomFilter<TEntity, int, TCount>[] StrataFilters { get; } =
           new InvertibleBloomFilter<TEntity, int, TCount>[MaxTrailingZeros];

        /// <summary>
        /// Configuration
        /// </summary>
        protected IBloomFilterConfiguration<TEntity, int, int, int, TCount> Configuration { get; }

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
            IBloomFilterConfiguration<TEntity,int,int,int,TCount> configuration)
        {
            EntityHash = e => configuration.EntityHashes(e, 1).First();
            _capacity = capacity;
            Configuration = configuration;
            DecodeCountFactor = _capacity >= 20 ? 1.39D : 1.0D;
        }
        #endregion

        #region Implementation of IStrataEstimator{TEntity, TId, TCount}
        /// <summary>
        /// Extract the data
        /// </summary>
        /// <returns></returns>
        public StrataEstimatorData<int, TCount> Extract()
        {
            var result = new StrataEstimatorData<int, TCount> {
                Capacity = _capacity,
                DecodeCountFactor = DecodeCountFactor,
                BloomFilters = new InvertibleBloomFilterData<int,int,TCount>[MaxTrailingZeros]
            };
            for(var i=0; i < StrataFilters.Length; i++)
            {
                if (StrataFilters[i] == null) continue;
               result.BloomFilters[i] = StrataFilters[i].Extract();
            }
            return result;
        }

        /// <summary>
        /// Add an item to strata estimator.
        /// </summary>
        /// <param name="item">Item to add</param>
        public virtual void Add(TEntity item)
        {
            Add(item, NumTrailingBinaryZeros(EntityHash(item)));
        }

        /// <summary>
        /// Remove an item from the strata estimator.
        /// </summary>
        /// <param name="item">Item to remove</param>
        public virtual void Remove(TEntity item)
        {
            Remove(item, NumTrailingBinaryZeros(EntityHash(item)));
        }

        /// <summary>
        /// Decode the given estimator data.
        /// </summary>
        /// <param name="estimator">Estimator data to subtract.</param>
        /// <param name="destructive">When <c>true</c> the values in this estimator will be altered and rendered useless, else <c>false</c>.</param>
        /// <returns></returns>
        public virtual ulong Decode(IStrataEstimatorData<int, TCount> estimator,
            bool destructive = false)
        {
            return Extract().Decode(estimator, Configuration, destructive);
        }

        /// <summary>
        /// Decode
        /// </summary>
        /// <param name="estimator">Other estimator.</param>
        /// <param name="destructive">When <c>true</c> the values in this estimator will be altered and rendered useless, else <c>false</c>.</param>
        /// <returns></returns>
        public virtual ulong Decode(IStrataEstimator<TEntity, int, TCount> estimator,
            bool destructive = false)
        {
            return Decode(estimator.Extract(), destructive);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Determine the number of trailing zeros in the bit representation of <paramref name="n"/>.
        /// </summary>
        /// <param name="n">The number</param>
        /// <returns>number of trailing zeros.</returns>
        protected static int NumTrailingBinaryZeros(long n)
        {
            var mask = 1;
            for (var i = 0; i < MaxTrailingZeros; i++, mask <<= 1)
                if ((n & mask) != 0)
                    return i;
            return MaxTrailingZeros;
        }

        /// <summary>
        /// Remove an item from the given strata estimator 
        /// </summary>
        /// <param name="item">Item to remove</param>
        /// <param name="idx">Index of the strata estimator</param>
        protected void Remove(TEntity item, long idx)
        {
            StrataFilters[idx]?.Remove(item);
        }

        /// <summary>
        /// Add an item to the given strata estimator
        /// </summary>
        /// <param name="item">Item to add</param>
        /// <param name="idx">Index of the strata estimator</param>
        protected void Add(TEntity item, long idx)
        {
            if (StrataFilters[idx] == null)
            {
                StrataFilters[idx] = new InvertibleBloomFilter<TEntity, int, TCount>(_capacity, 0.001F, Configuration);
            }
            StrataFilters[idx].Add(item);
        }
        #endregion
    }
}
