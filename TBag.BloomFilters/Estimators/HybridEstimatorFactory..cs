namespace TBag.BloomFilters.Estimators
{
    using Configurations;
    using MathExt;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    /// <summary>
    /// Encapsulates emperical data for creating hybrid estimators.
    /// </summary>
    public class HybridEstimatorFactory : IHybridEstimatorFactory
    {
        /// <summary>
        /// Create a hybrid estimator
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="TCount">The type of occurence count.</typeparam>
        /// <param name="configuration">Bloom filter configuration</param>
        /// <param name="setSize">Number of elements in the set that is added.</param>
        /// <param name="failedDecodeCount">Number of times decoding has failed based upon the provided estimator.</param>
        /// <returns></returns>
        public IHybridEstimatorData<int, TCount> Extract<TEntity, TId, TCount>(
            IBloomFilterConfiguration<TEntity, TId, int, TCount> configuration,
             HybridEstimator<TEntity, TId, TCount> precalculatedEstimator,
            byte failedDecodeCount = 0)
            where TCount : struct
            where TId : struct
        {
            if (precalculatedEstimator == null) return null;
            var data = precalculatedEstimator.FullExtract();
            var minwiseHashCount = GetRecommendedMinwiseHashCount(configuration, data.ItemCount, failedDecodeCount);
            var strata = GetRecommendedStrata(configuration, data.ItemCount, failedDecodeCount);
            var capacity = GetRecommendedCapacity(configuration, data.ItemCount, failedDecodeCount);
            var bitSize = GetRecommendedBitSize(configuration, data.ItemCount, failedDecodeCount);
                var factors = MathExtensions.GetFactors(precalculatedEstimator.BlockSize);
            var foldFactor = capacity > 0L ?
                (uint)factors
                .OrderByDescending(f => f)
                .Where(f => precalculatedEstimator.BlockSize / f > capacity)
                .Skip(failedDecodeCount)
                .FirstOrDefault():
                0L;
            if (foldFactor > 1)
            {
                data = data.Fold(configuration, (uint)foldFactor);
            }
            data.StrataEstimator.LowerStrata(strata);
            if (failedDecodeCount > 1)
            {
                data.StrataEstimator.DecodeCountFactor = Math.Pow(2, failedDecodeCount);
            }
            return data.ToEstimatorData();
        }

        /// <summary>
        /// Create a hybrid estimator
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="TCount">The type of occurence count.</typeparam>
        /// <param name="configuration">Bloom filter configuration</param>
        /// <param name="setSize">Number of elements in the set that is added.</param>
        /// <param name="failedDecodeCount">Number of times decoding has failed based upon the provided estimator.</param>
        /// <returns></returns>
        public HybridEstimator<TEntity,TId, TCount> Create<TEntity, TId, TCount>(
            IBloomFilterConfiguration<TEntity, TId, int, TCount> configuration,
            long setSize,
            byte failedDecodeCount = 0)
            where TCount : struct
            where TId : struct
        {
            var minwiseHashCount = GetRecommendedMinwiseHashCount(configuration, setSize, failedDecodeCount);
            var strata = GetRecommendedStrata(configuration, setSize, failedDecodeCount);
            var capacity = GetRecommendedCapacity(configuration, setSize, failedDecodeCount);
            var bitSize = GetRecommendedBitSize(configuration, setSize, failedDecodeCount);
            var result = new HybridEstimator<TEntity, TId, TCount>(
                capacity,
                strata,
                configuration)
            { };
            result.Initialize(setSize, bitSize, minwiseHashCount);
            if (failedDecodeCount > 1)
            {
                result.DecodeCountFactor = Math.Pow(2, failedDecodeCount);
            }
            return result;
        }

        /// <summary>
        /// Get the recommended minwise hash count.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="THash"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <param name="configuration"></param>
        /// <param name="setSize"></param>
        /// <param name="failedDecodeCount"></param>
        /// <returns></returns>
        public int GetRecommendedMinwiseHashCount<TEntity, TId, THash, TCount>(IBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration,
         long setSize,
         byte failedDecodeCount = 0)
          where TId : struct
         where THash : struct
         where TCount : struct
        {
            var minwiseHashCount = 8;
            if (setSize > 16000L)
            {
                minwiseHashCount = 15;
            }
            else if (setSize > 8000L)
            {
                minwiseHashCount = 10;
            }
            return minwiseHashCount;
        }

        /// <summary>
        /// Get the recommended bit size.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="THash"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <param name="configuration"></param>
        /// <param name="setSize"></param>
        /// <param name="failedDecodeCount"></param>
        /// <returns></returns>
        public byte GetRecommendedBitSize<TEntity, TId, THash, TCount>(IBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration,
         long setSize,
         byte failedDecodeCount = 0)
          where TId : struct
         where THash : struct
         where TCount : struct
        {
            byte bitSize = 2;
            return bitSize;
        }

        /// <summary>
        /// Get recommended strata.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="THash"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <param name="configuration"></param>
        /// <param name="setSize"></param>
        /// <param name="failedDecodeCount"></param>
        /// <returns></returns>
        public byte GetRecommendedStrata<TEntity, TId, THash, TCount>(IBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration,
            long setSize,
            byte failedDecodeCount = 0)
             where TId : struct
            where THash : struct
            where TCount : struct
        {
            byte strata = 7;
            if (setSize > 16000L)
            {
                strata = 13;
            }
            else if (setSize > 8000L)
            {
                strata = 9;
            }
           if (failedDecodeCount >= 1)
            {
                strata = (byte)(setSize > 10000L || failedDecodeCount > 1
                    ? 13
                    : 9);
            }
            return strata;
        }

        /// <summary>
        /// Determine the size of the estimator based upon the number of elements and the number of failed attempts.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="THash"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <param name="configuration"></param>
        /// <param name="setSize"></param>
        /// <param name="failedDecodeCount"></param>
        /// <returns></returns>
        public long GetRecommendedCapacity<TEntity, TId, THash, TCount>(IBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration,
            long setSize,
            byte failedDecodeCount = 0)
            where TId : struct
            where THash : struct
            where TCount : struct
        {
            var capacity = (long)(50 * Math.Max(1.0D, Math.Log10(setSize)));
            if (setSize > 16000L &&
                capacity < 1000)
            {
                capacity = 1000;
            }
            if (failedDecodeCount > 1 &&
               capacity < (long)0.2D * setSize)
            {
                capacity = failedDecodeCount < 2
                    ? (long)0.2D * setSize
                    : (long)0.5D * setSize;
            }
            return capacity;

        }

        /// <summary>
        /// Create an estimator that matches the given <paramref name="data"/> estimator.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <typeparam name="TId">The type of the identifier</typeparam>
        /// <typeparam name="TCount">The type of the occurence count</typeparam>
        /// <param name="data">The estimator data to match</param>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <param name="setSize">The (estimated) size of the set to add to the estimator.</param>
        /// <returns>An estimator</returns>
        public IHybridEstimator<TEntity, int, TCount> CreateMatchingEstimator<TEntity, TId, TCount>(
            IHybridEstimatorData<int, TCount> data,
            IBloomFilterConfiguration<TEntity, TId, int,  TCount> configuration,
            long setSize)
            where TCount : struct
            where TId : struct
        {
            var estimator = new HybridEstimator<TEntity, TId, TCount>(
                data.StrataEstimator.BlockSize,
                data.StrataEstimator.StrataCount,
                configuration)
            {
                DecodeCountFactor = data.StrataEstimator.DecodeCountFactor
            };
            if (data.BitMinwiseEstimator != null)
            {
                estimator.Initialize(setSize, data.BitMinwiseEstimator.BitSize, data.BitMinwiseEstimator.HashCount);
            }
            return estimator;
        }
    }
}
