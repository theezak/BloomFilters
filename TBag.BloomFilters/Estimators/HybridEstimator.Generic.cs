namespace TBag.BloomFilters.Estimators
{
    using System;

    /// <summary>
    /// A hybrid estimator with a limited strata that cuts over to a bit minwise estimator.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TId">The identifier type</typeparam>
    /// <typeparam name="TCount">The type of occurence count.</typeparam>
    public class HybridEstimator<TEntity, TId, TCount> : 
        StrataEstimator<TEntity, TCount>,
        IHybridEstimator<TEntity, int, TCount> 
        where TCount : struct
        where TId : struct
    {
        #region Fields
        private readonly BitMinwiseHashEstimator<TEntity, int, TCount> _minwiseEstimator;
        private byte _maxStrata;
        private long _capacity;
        private long _setSize;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="capacity">Capacity for strata estimator (good default is 80)</param>
        /// <param name="bitSize">The bit size for the bit minwise estimator.</param>
        /// <param name="minWiseHashCount">number of hash functions for the bit minwise estimator</param>
        /// <param name="setSize">Estimated maximum set size for the bit minwise estimator (capacity)</param>
        /// <param name="maxStrata">Maximum strate for the strata estimator.</param>
        /// <param name="configuration">The configuration</param>
        public HybridEstimator(
            long capacity,
            byte bitSize,
            int minWiseHashCount,
            long setSize,
            byte maxStrata,
            IBloomFilterConfiguration<TEntity, TId, int, int, TCount> configuration) : base(
                capacity, 
                configuration.ConvertToEntityHashId())        
        {
            _capacity = capacity;
            _maxStrata = maxStrata;
            var max = Math.Pow(2, MaxTrailingZeros);
             _setSize = setSize;
            //TODO: clean up math. This is very close though to what actually ends up in the estimator.
            var inStrata = max - Math.Pow(2, MaxTrailingZeros - maxStrata);           
            _minwiseEstimator = new BitMinwiseHashEstimator<TEntity, int, TCount>(
                Configuration, 
                bitSize, 
                minWiseHashCount, 
                Math.Max((uint)(_setSize * (1 - inStrata / max)), 1));
            DecodeCountFactor = _capacity >= 20 ? 1.45D : 1.0D;
        }
        #endregion

        #region Implementation
        /// <summary>
        /// Add an item to the estimator.
        /// </summary>
        /// <param name="item"></param>
        /// <remarks>based upon the strata, the value is either added to an IBF or to the b-bit minwise estimator.</remarks>
        public override void Add(TEntity item)
        {
            var idx = NumTrailingBinaryZeros(EntityHash(item));
            if (idx < _maxStrata)
            {
                Add(item, idx);
            }
            else
            {
                _minwiseEstimator.Add(item);
            }
        }

        /// <summary>
        /// Remove an item from the estimator
        /// </summary>
        /// <param name="item"></param>
        /// <exception cref="NotSupportedException">Removal is not supported on a hybrid estimator that utilizes the minwise estimator.</exception>
        public override void Remove(TEntity item)
        {
            if (_maxStrata < MaxTrailingZeros)
            {
                throw new NotSupportedException("Removal not supported on a hybrid estimator.");
            }
            base.Remove(item);
        }

        /// <summary>
        /// Extract the hybrid estimator in a serializable format.
        /// </summary>
        /// <returns></returns>
        IHybridEstimatorData<int, TCount> IHybridEstimator<TEntity, int, TCount>.Extract()
        {
            return new HybridEstimatorData<int, TCount>
            {
                Capacity = _capacity,
                BitMinwiseEstimator = _minwiseEstimator.Extract(),
                StrataEstimator = Extract(),
                StrataCount = _maxStrata,
                CountEstimate = _setSize
            };
        }

        /// <summary>
        /// Extract the full estimator data
        /// </summary>
        /// <remarks>Do not serialize across the wire, but can be used to rehydrate an estimator.</remarks>
        /// <returns></returns>
        IHybridEstimatorFullData<int, TCount> IHybridEstimator<TEntity, int, TCount>.FullExtract()
        {
            return new HybridEstimatorFullData<int, TCount>
            {
                Capacity = _capacity,
                BitMinwiseEstimator = _minwiseEstimator.FullExtract(),
                StrataEstimator = Extract(),
                StrataCount = _maxStrata,
                CountEstimate = _setSize
            };
        }

        /// <summary>
        /// Rehydrate the hybrid estimator from full data.
        /// </summary>
        /// <param name="data"></param>
        void IHybridEstimator<TEntity, int, TCount>.Rehydrate(IHybridEstimatorFullData<int, TCount> data)
        {
            if (data == null) return;
            _minwiseEstimator.Rehydrate(data.BitMinwiseEstimator);
            _capacity = data.Capacity;
            _maxStrata = data.StrataCount;
            _setSize = data.CountEstimate;
            Rehydrate(data.StrataEstimator);
        }

        /// <summary>
        /// Decode the given hybrid estimator.
        /// </summary>
        /// <param name="estimator">The estimator for the other set.</param>
        /// <param name="destructive">When <c>true</c> the values in this estimator will be altered and rendered useless, else <c>false</c>.</param>
        /// <returns>An estimate of the number of differences between the two sets that the estimators are based upon.</returns>

        public long? Decode(IHybridEstimator<TEntity, int, TCount> estimator,
            bool destructive = false)
         {
             if (estimator == null) return _capacity;
            return Decode(estimator.Extract(), destructive);
        }

        /// <summary>
        /// Decode the given hybrid estimator data.
        /// </summary>
        /// <param name="estimator">The estimator for the other set.</param>
        /// <param name="destructive">When <c>true</c> the values in this estimator will be altered and rendered useless, else <c>false</c>.</param>
        /// <returns>An estimate of the number of differences between the two sets that the estimators are based upon.</returns>
        public long? Decode(IHybridEstimatorData<int, TCount> estimator,
            bool destructive = false)
        {
            if (estimator == null) return _setSize;
            return ((IHybridEstimator<TEntity, int, TCount>) this)
                .Extract()
                .Decode(estimator, Configuration);
        }

        
       
        #endregion
    }
}
