namespace TBag.BloomFilters.Estimators
{
    using System;

    /// <summary>
    /// A hybrid estimator with a limited strata that cuts over to a bit minwise estimator.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TId">The entity identifier type</typeparam>
    /// <typeparam name="TCount">The type of occurence count.</typeparam>
    public class HybridEstimator<TEntity, TId, TCount> : 
        StrataEstimator<TEntity, TId, TCount>, IHybridEstimator<TEntity, TId, TCount> where TCount : struct
    {
        #region Fields
        private readonly BitMinwiseHashEstimator<TEntity, TId, TCount> _minwiseEstimator;
        private readonly int _maxStrata;
        private readonly long _capacity;
        private readonly ulong _setSize;
        #endregion

        #region Properties
       /// <summary>
       /// The decode count factor.
       /// </summary>
        protected override double DecodeCountFactor => _capacity >= 20 ? 1.45D : 1.0D;
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
            ulong setSize,
            byte maxStrata,
            IBloomFilterConfiguration<TEntity, int, TId, long, TCount> configuration) : base(capacity, configuration)
        {
            _capacity = capacity;
            _maxStrata = maxStrata;
            var max = Math.Pow(2, MaxTrailingZeros);
            var inStrata = max - Math.Pow(2, MaxTrailingZeros - maxStrata);
            //TODO: clean up math.
            _setSize = (uint)(setSize * (1-(inStrata / max)));
            _minwiseEstimator = new BitMinwiseHashEstimator<TEntity, TId, TCount>(configuration, bitSize, minWiseHashCount, Math.Max(_setSize,1));
        }
        #endregion

        #region Implementation
        /// <summary>
        /// Add an item to the estimator.
        /// </summary>
        /// <param name="item"></param>
        public override void Add(TEntity item)
        {
            var idx = NumTrailingBinaryZeros(IdHash(Configuration.GetId(item)));
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
        public IHybridEstimatorData<TId, TCount> ExtractHybrid()
        {
            return new HybridEstimatorData<TId, TCount>
            {
                Capacity = _capacity,
                BitMinwiseEstimator = _minwiseEstimator.Extract(),
                StrataEstimator = Extract(),
                StrataCount = _maxStrata
            };
        }
         /// <summary>
        /// Decode the given hybrid estimator.
        /// </summary>
        /// <param name="estimator"></param>
        /// <returns>Estimated number of elements that are different.</returns>
        public ulong Decode(IHybridEstimator<TEntity, TId, TCount> estimator)
         {
             if (estimator == null) return (ulong)_capacity;
            return Decode(estimator.ExtractHybrid());
        }

        /// <summary>
        /// Decode the given hybrid estimator data.
        /// </summary>
        /// <param name="estimator"></param>
        /// <returns></returns>
        public ulong Decode(IHybridEstimatorData<TId, TCount> estimator)
        {
            if (estimator == null) return (ulong)_capacity;
            return ExtractHybrid().Decode(estimator, Configuration);
        }
        #endregion
    }
}
