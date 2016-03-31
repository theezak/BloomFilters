﻿namespace TBag.BloomFilters.Estimators
{
    using Configurations;
    using System;
    using System.Collections.Generic;
    /// <summary>
    /// A hybrid estimator with a limited strata that cuts over to a bit minwise estimator.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TId">The identifier type</typeparam>
    /// <typeparam name="TCount">The type of occurence count.</typeparam>
    public class HybridEstimator<TEntity, TId, TCount> : 
        StrataEstimator<TEntity, TId, TCount>,
        IHybridEstimator<TEntity, int, TCount> 
        where TCount : struct
        where TId : struct
    {
        #region Fields
        private readonly BitMinwiseHashEstimator<KeyValuePair<int,int>, int, TCount> _minwiseEstimator;
        #endregion

        #region Properties
        /// <summary>
        /// The item count for the estimator.
        /// </summary>
        public override long ItemCount => base.ItemCount + (_minwiseEstimator?.ItemCount ?? 0L);

        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="blockSize">Capacity for strata estimator (good default is 80)</param>
        /// <param name="bitSize">The bit size for the bit minwise estimator.</param>
        /// <param name="minWiseHashCount">number of hash functions for the bit minwise estimator</param>
        /// <param name="setSize">Estimated maximum set size for the whole estimator</param>
        /// <param name="maxStrata">Maximum strate for the strata estimator.</param>
        /// <param name="configuration">The configuration</param>
        public HybridEstimator(
            long blockSize,
            byte bitSize,
            int minWiseHashCount,
            long setSize,
            byte maxStrata,
            IBloomFilterConfiguration<TEntity, TId,  int, TCount> configuration) : base(
                blockSize,
                configuration,
                maxStrata)        
        {
            //TODO: clean up math. This is very close though to what actually ends up in the estimator.
            var max = Math.Pow(2, MaxTrailingZeros);
            var minWiseCapacity = Math.Max(
                (uint) (setSize*(1 - (max - Math.Pow(2, MaxTrailingZeros - maxStrata))/max)), 1);
            if (configuration.FoldingStrategy != null)
            {
                minWiseCapacity = (uint)configuration.FoldingStrategy.ComputeFoldableSize(minWiseCapacity, 2);
            }
            _minwiseEstimator = new BitMinwiseHashEstimator<KeyValuePair<int,int>, int, TCount>(
                Configuration.ConvertToEstimatorConfiguration(), 
                bitSize, 
                minWiseHashCount, 
                minWiseCapacity);
            DecodeCountFactor = BlockSize >= 20 ? 1.45D : 1.0D;
        }
        #endregion

        #region Implementation
        /// <summary>
        /// Add an item to the estimator.
        /// </summary>
        /// <param name="item">The entity to add</param>
        /// <remarks>based upon the strata, the value is either added to an IBF or to the b-bit minwise estimator.</remarks>
        public override void Add(TEntity item)
        {
            var idHash = Configuration.IdHash(Configuration.GetId(item));
            var entityHash = Configuration.EntityHash(item);
            var idx = GetStrata(idHash, entityHash);
            if (idx < MaxStrata)
            {
                Add(idHash, entityHash, idx);
            }
            else
            {
                _minwiseEstimator.Add(new KeyValuePair<int, int>(idHash, entityHash));
            }
        }

        /// <summary>
        /// Remove an item from the estimator
        /// </summary>
        /// <param name="item"></param>
        /// <exception cref="NotSupportedException">Removal is not supported on a hybrid estimator that utilizes the minwise estimator.</exception>
        public override void Remove(TEntity item)
        {
            if (MaxStrata < MaxTrailingZeros)
            {
                throw new NotSupportedException("Removal not supported on a hybrid estimator.");
            }
            base.Remove(item);
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
            if (estimator == null) return ItemCount;
            return ((IHybridEstimator<TEntity, int, TCount>) this)
                .Extract()
                .Decode(estimator, Configuration);
        }

        /// <summary>
        /// Fold the hybrid estimator.
        /// </summary>
        /// <param name="factor"></param>
        /// <param name="inPlace"></param>
        /// <returns></returns>
        IHybridEstimator<TEntity, int, TCount> IHybridEstimator<TEntity, int, TCount>.Fold(uint factor, bool inPlace)
        {
            IHybridEstimator<TEntity, int, TCount> self = this;
            
            var res = self.FullExtract().Fold(Configuration, factor);
            if (inPlace)
            {
                self.Rehydrate(res);
                return this;
            }
            var max = Math.Pow(2, MaxTrailingZeros);
            //TODO: estimator constructor that simply takes the full data? Split things across constructor and initialize?
            IHybridEstimator<TEntity, int, TCount> estimator = new HybridEstimator<TEntity, TId, TCount>(
                res.BlockSize,
                res.BitMinwiseEstimator.BitSize, 
                res.BitMinwiseEstimator.HashCount, 
                res.BitMinwiseEstimator.ItemCount, 
                res.StrataCount, 
                Configuration);
            estimator.Rehydrate(res);
            return estimator;
        }

        /// <summary>
        /// Compress the hybrid estimator.
        /// </summary>
        /// <param name="inPlace"></param>
        /// <returns></returns>
        IHybridEstimator<TEntity, int, TCount> IHybridEstimator<TEntity, int, TCount>.Compress(bool inPlace)
        {
            IHybridEstimator<TEntity, int, TCount> self = this;
            var res = self.FullExtract().Compress(Configuration);
            if (inPlace)
            {
                self.Rehydrate(res);
                return this;
            }
            var max = Math.Pow(2, MaxTrailingZeros);
            IHybridEstimator<TEntity, int, TCount> estimator = new HybridEstimator<TEntity, TId, TCount>(
                res.BlockSize,
                res.BitMinwiseEstimator.BitSize,
                res.BitMinwiseEstimator.HashCount,
                (long)(res.BitMinwiseEstimator.Capacity * (1 + (max - Math.Pow(2, MaxTrailingZeros - res.StrataCount)) / max)), res.StrataCount, Configuration);
            estimator.Rehydrate(res);
            return estimator;
        }
        #endregion

        #region Implementation of IHybridEstimator{Entity, int, TCount}
        /// <summary>
        /// Extract the hybrid estimator in a serializable format.
        /// </summary>
        /// <returns></returns>
        IHybridEstimatorData<int, TCount> IHybridEstimator<TEntity, int, TCount>.Extract()
        {
            return new HybridEstimatorData<int, TCount>
            {
                BlockSize = BlockSize,
                BitMinwiseEstimator = _minwiseEstimator.Extract(),
                StrataEstimator = Extract(),
                StrataCount = MaxStrata,
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
                BlockSize = BlockSize,
                BitMinwiseEstimator = _minwiseEstimator.FullExtract(),
                StrataEstimator = Extract(),
                StrataCount = MaxStrata               
            };
        }

        /// <summary>
        /// Rehydrate the hybrid estimator from full data.
        /// </summary>
        /// <param name="data">The data to restore</param>
        void IHybridEstimator<TEntity, int, TCount>.Rehydrate(IHybridEstimatorFullData<int, TCount> data)
        {
            if (data == null) return;
            _minwiseEstimator.Rehydrate(data.BitMinwiseEstimator);
            BlockSize = data.BlockSize;
            MaxStrata = data.StrataCount;
            Rehydrate(data.StrataEstimator);
        }     
        #endregion
    }
}
