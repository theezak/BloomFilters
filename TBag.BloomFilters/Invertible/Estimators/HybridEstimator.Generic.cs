namespace TBag.BloomFilters.Invertible.Estimators
{
    using BloomFilters.Estimators;
    using Configurations;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A hybrid estimator with a limited strata that cuts over to a bit minwise estimator.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TId">The identifier type</typeparam>
    /// <typeparam name="TCount">The type of occurence count.</typeparam>
    public sealed class HybridEstimator<TEntity, TId, TCount> : 
        IHybridEstimator<TEntity, int, TCount> 
        where TCount : struct
        where TId : struct
    {
        #region Fields
        private long _minwiseReplacementCount;
        private readonly IInvertibleBloomFilterConfiguration<TEntity, TId, int, TCount> _configuration;
        private BitMinwiseHashEstimator<KeyValuePair<int, int>, int, TCount> _minwiseEstimator;
        private readonly StrataEstimator<TEntity, TId, TCount> _strataEstimator;
        #endregion

        #region Properties
        /// <summary>
        /// The item count for the estimator.
        /// </summary>
        public long ItemCount => _strataEstimator.ItemCount + (_minwiseEstimator?.ItemCount ?? 0L) + _minwiseReplacementCount;

        /// <summary>
        /// The block size
        /// </summary>
        public long BlockSize => _strataEstimator.BlockSize;

        /// <summary>
        /// The virtual block size that drives the estimator its behavior.
        /// </summary>
        public long VirtualBlockSize => _strataEstimator.BlockSize*_strataEstimator.MaxStrata;

        /// <summary>
        /// The hash function count
        /// </summary>
        public uint HashFunctionCount => _strataEstimator.HashFunctionCount;

        /// <summary>
        /// The decode factor.
        /// </summary>
        public double DecodeCountFactor
        {
            get { return _strataEstimator.DecodeCountFactor;  }
            set { _strataEstimator.DecodeCountFactor = value; }
        }

        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="blockSize">Capacity for strata estimator (good default is 80)</param>
          /// <param name="maxStrata">Maximum strate for the strata estimator.</param>
        /// <param name="configuration">The configuration</param>
        public HybridEstimator(
            long blockSize,
            byte maxStrata,
            IInvertibleBloomFilterConfiguration<TEntity, TId,  int, TCount> configuration) 
        {
            _strataEstimator = new StrataEstimator<TEntity, TId, TCount>(blockSize, configuration, maxStrata);
            _strataEstimator.DecodeCountFactor = _strataEstimator.BlockSize >= 20 ? 1.45D : 1.0D;
            _configuration = configuration;
        }
        #endregion

        #region Implementation
        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="capacity">The capacity (number of items to be added)</param>
        /// <param name="bitSize">The bit size for the bit minwise estimator</param>
        /// <param name="minWiseHashCount">The minwise hash count</param>
        public void Initialize(
            long capacity,
            byte bitSize,
            int minWiseHashCount)
        {
            var max = Math.Pow(2, _strataEstimator.StrataLimit);
            var minWiseCapacity = Math.Max(
                (uint)(capacity * (1 - (max - Math.Pow(2, _strataEstimator.StrataLimit - _strataEstimator.MaxStrata)) / max)), 1);
            if (_configuration.FoldingStrategy != null)
            {
                minWiseCapacity = (uint)_configuration.FoldingStrategy.ComputeFoldableSize(minWiseCapacity, 2);
            }
            _minwiseEstimator = new BitMinwiseHashEstimator<KeyValuePair<int, int>, int, TCount>(
                _configuration.ConvertToEstimatorConfiguration(),
                bitSize,
                minWiseHashCount,
                minWiseCapacity);
            _minwiseReplacementCount = 0L;
        }

        /// <summary>
        /// Add an item to the estimator.
        /// </summary>
        /// <param name="item">The entity to add</param>
        /// <remarks>based upon the strata, the value is either added to an IBF or to the b-bit minwise estimator.</remarks>
        public void Add(TEntity item)
        {
            var idHash = _configuration.IdHash(_configuration.GetId(item));
            var entityHash = _configuration.EntityHash(item);
            if (!_strataEstimator.ConditionalAdd(idHash, entityHash))
            {
                if (_minwiseEstimator == null)
                {
                    _minwiseReplacementCount++;
                }
                else
                {
                    _minwiseEstimator.Add(new KeyValuePair<int, int>(idHash, entityHash));
                }
            }
        }

        /// <summary>
        /// Extract the hybrid estimator data.
        /// </summary>
        /// <returns></returns>
        public IHybridEstimatorData<int, TCount> Extract()
        {
            return new HybridEstimatorData<int, TCount>
            {
                ItemCount = ItemCount,
                BitMinwiseEstimator = _minwiseEstimator?.Extract(),
                StrataEstimator = _strataEstimator.Extract()
            };
        }

        /// <summary>
        /// Remove an item from the estimator
        /// </summary>
        /// <param name="item">Item to remove</param>
        public void Remove(TEntity item)
        {
            if (_strataEstimator.MaxStrata < _strataEstimator.StrataLimit &&
                _minwiseEstimator != null)
            {
                _minwiseReplacementCount += _minwiseEstimator.ItemCount;
                //after removal, the bit minwise estimator needs to be dropped since we can't remove from that.
                _minwiseEstimator = null;
            }
            var idHash = _configuration.IdHash(_configuration.GetId(item));
            var entityHash = _configuration.EntityHash(item);
            if (!_strataEstimator.ConditionalRemove(idHash, entityHash))
            {
                _minwiseReplacementCount--;
            }
        }

        /// <summary>
        /// Determine if the item is in the estimator.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(TEntity item)
        {            
             //if the strata estimator can't determine membership, assume it is in there (and count it toward false positives)
             //this only happens for a very small subset of items, that have a strata above the maximum strata of the estimator.
            return _strataEstimator.Contains(item) ?? true;
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
            if (estimator == null) return ItemCount;
            return Decode(estimator.Extract(),  destructive);
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
            if (estimator == null) return ItemCount;
            IHybridEstimator<TEntity, int, TCount> self = this;
            return self
                .Extract()
                .Decode(estimator, _configuration);
        }

        /// <summary>
        /// intersect with the estimator
        /// </summary>
        /// <param name="estimator"></param>
        public void Intersect(IHybridEstimator<TEntity, int, TCount> estimator)
        {
            Intersect(estimator.FullExtract());
        }


        /// <summary>
        /// Intersect with the estimator.
        /// </summary>
        /// <param name="estimator"></param>
        public void Intersect(IHybridEstimatorFullData<int,TCount> estimator)
        {
            IHybridEstimator<TEntity, int, TCount> self = this;
            Rehydrate(self
                .FullExtract()
                .Intersect(_configuration, estimator));
        }
        #endregion

        #region Implementation of IHybridEstimator{Entity, int, TCount}
        /// <summary>
        /// Fold the hybrid estimator.
        /// </summary>
        /// <param name="factor"></param>
        /// <param name="inPlace"></param>
        /// <returns></returns>
        public IHybridEstimator<TEntity, int, TCount> Fold(uint factor, bool inPlace)
        {
            IHybridEstimator<TEntity, int, TCount> self = this;
            var res = FullExtract().Fold(_configuration, factor);
            if (inPlace)
            {
                self.Rehydrate(res);
                return this;
            }
            IHybridEstimator<TEntity, int, TCount> estimator = new HybridEstimator<TEntity, TId, TCount>(
                res.StrataEstimator.BlockSize,
                res.StrataEstimator.StrataCount,
                _configuration);
            if (res.BitMinwiseEstimator != null)
            {
                estimator.Initialize(
                    res.BitMinwiseEstimator.ItemCount,
                    res.BitMinwiseEstimator.BitSize,
                    res.BitMinwiseEstimator.HashCount);
            }
            estimator.Rehydrate(res);
            return estimator;
        }

        /// <summary>
        /// Compress the hybrid estimator.
        /// </summary>
        /// <param name="inPlace"></param>
        /// <returns></returns>
        public IHybridEstimator<TEntity, int, TCount> Compress(bool inPlace)
        {
            IHybridEstimator<TEntity, int, TCount> self = this;
            var res = FullExtract().Compress(_configuration);
            if (inPlace)
            {
                self.Rehydrate(res);
                return this;
            }
            IHybridEstimator<TEntity, int, TCount> estimator = new HybridEstimator<TEntity, TId, TCount>(
                res.StrataEstimator.BlockSize,
                res.StrataEstimator.StrataCount,
                _configuration);
            if (res.BitMinwiseEstimator != null)
            {
                estimator.Initialize(
                    res.BitMinwiseEstimator.Capacity + res.ItemCount,
                    res.BitMinwiseEstimator.BitSize,
                    res.BitMinwiseEstimator.HashCount);
            }
            estimator.Rehydrate(res);
            return estimator;
        }

        /// <summary>
        /// Extract the full estimator data
        /// </summary>
        /// <remarks>Do not serialize across the wire, but can be used to rehydrate an estimator.</remarks>
        /// <returns></returns>
        public HybridEstimatorFullData<int, TCount> FullExtract()
        {
            return new HybridEstimatorFullData<int, TCount>
            {
                ItemCount = ItemCount,
                BitMinwiseEstimator = _minwiseEstimator?.FullExtract(),
                StrataEstimator = _strataEstimator.Extract()           
            };
        }

        IHybridEstimatorFullData<int, TCount> IHybridEstimator<TEntity, int, TCount>.FullExtract()
        {
            return FullExtract();
        }

        /// <summary>
        /// Rehydrate the hybrid estimator from full data.
        /// </summary>
        /// <param name="data">The data to restore</param>
        public void Rehydrate(IHybridEstimatorFullData<int, TCount> data)
        {
            if (data == null) return;
            _minwiseEstimator?.Rehydrate(data.BitMinwiseEstimator);
            _strataEstimator.Rehydrate(data.StrataEstimator);
            _minwiseReplacementCount = Math.Max(0, data.ItemCount - (_strataEstimator.ItemCount + (_minwiseEstimator?.ItemCount ?? 0L)));
        }

        /// <summary>
        /// Rehydrate the hybrid estimator 
        /// </summary>
        /// <param name="data">The data to restore</param>
        /// <remarks>This rehydrate is lossy, since it can't restore the bit minwise estimator.</remarks>
        public void Rehydrate(IHybridEstimatorData<int, TCount> data)
        {
            if (data == null) return;
            _minwiseEstimator = null;
            _strataEstimator.Rehydrate(data.StrataEstimator);
            _minwiseReplacementCount = Math.Max(0, data.ItemCount - (_strataEstimator.ItemCount + (_minwiseEstimator?.ItemCount ?? 0L)));
        }
        #endregion
    }
}
