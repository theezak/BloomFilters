namespace TBag.BloomFilters.Estimators
{
    using System.Collections;
    using System.Linq;
    using System;
     using Configurations;

    /// <summary>
    /// Extension methods for bit minwise hash estimator data
    /// </summary>
    public static class BitMinwiseHashEstimatorDataExtensions
    {
        /// <summary>
        /// Determine the similarity between two estimators.
        /// </summary>
        /// <param name="estimator"></param>
        /// <param name="otherEstimatorData"></param>
        /// <returns>Similarity (percentage similar, zero is completely different, one is completely the same)</returns>
        /// <remarks>Zero is no similarity, one is completely similar.</remarks>
        public static double Similarity(this IBitMinwiseHashEstimatorData estimator,
            IBitMinwiseHashEstimatorData otherEstimatorData)
        {
            if (estimator == null ||
                otherEstimatorData == null ||
                estimator.BitSize != otherEstimatorData.BitSize ||
                estimator.HashCount != otherEstimatorData.HashCount) return 0.0D;
            if (estimator.Values == null && otherEstimatorData.Values == null) return 1.0D;           
            return ComputeSimilarityFromSignatures(
                CreateBitArray(estimator),
                CreateBitArray(otherEstimatorData),
                estimator.HashCount,
                estimator.BitSize);
        }

        /// <summary>
        /// Compress the estimator data.
        /// </summary>
        /// <param name="estimator"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IBitMinwiseHashEstimatorFullData Compress<TEntity, TId, TCount>(
            this IBitMinwiseHashEstimatorFullData estimator,
           IBloomFilterConfiguration<TEntity, TId, int, TCount> configuration)
            where TId : struct
            where TCount : struct
        {
            if (configuration?.FoldingStrategy == null || estimator == null) return null;
            var fold = configuration.FoldingStrategy.FindFoldFactor(estimator.Capacity, estimator.Capacity, estimator.ItemCount);
            var res = fold.HasValue ? estimator.Fold(fold.Value) : null;
            return res;
        }

        /// <summary>
        /// Fold the minwise estimator data.
        /// </summary>
        /// <param name="estimator">The estimator data</param>
         /// <param name="factor">The folding factor</param>
        /// <returns></returns>
        public static BitMinwiseHashEstimatorFullData Fold(
            this IBitMinwiseHashEstimatorFullData estimator,           
            uint factor)
        {
            if (factor <= 0)
                throw new ArgumentException($"Fold factor should be a positive number (given value was {factor}).");
            if (estimator == null) return null;
            if (estimator.Capacity % factor != 0)
                throw new ArgumentException($"Bit minwise filter data cannot be folded by {factor}.", nameof(factor));
            var res = new BitMinwiseHashEstimatorFullData
            {
                BitSize = estimator.BitSize,
                Capacity =  estimator.Capacity / factor,
                HashCount = estimator.HashCount,
                ItemCount = estimator.ItemCount,
                Values = estimator.Values==null?null:new int[estimator.Capacity / factor]
            };
            if ((res.Values?.Length??0L) == 0L) return res;
            for (var i = 0L; i < estimator.Values.LongLength; i++)
            {
                if (i < res.Values.LongLength)
                {
                    res.Values[i] = estimator.Values[i];
                }
                else
                {
                    var pos = i % res.Values.LongLength;
                    if (res.Values[pos] > estimator.Values[i])
                    {
                        res.Values[pos] = estimator.Values[i];
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Add two estimators.
        /// </summary>
        /// <param name="estimator">The estimator to add to.</param>
        /// <param name="otherEstimator">The other estimator to add.</param>
        /// <param name="foldingStrategy">THe folding strategy to use</param>
        /// <param name="inPlace">When <c>true</c> the data is added to the given <paramref name="estimator"/>, otherwise a new estimator is created.</param>
        /// <returns></returns>
        public static IBitMinwiseHashEstimatorFullData Add(
            this IBitMinwiseHashEstimatorFullData estimator,
            IBitMinwiseHashEstimatorFullData otherEstimator,
            IFoldingStrategy foldingStrategy,
            bool inPlace = false)
        {
            if (estimator == null ||
                otherEstimator == null) return null;
            var foldingFactors = foldingStrategy?.GetFoldFactors(estimator.Capacity, otherEstimator.Capacity);
            if ((estimator.Capacity != otherEstimator.Capacity && 
                (foldingFactors?.Item1??0L)<=1 &&
                 (foldingFactors?.Item2 ?? 0L) <= 1) ||
                estimator.HashCount != otherEstimator.HashCount)
            {
                throw new ArgumentException("Minwise estimators with different capacity or hash count cannot be added.");
            }
            var res = inPlace &&
                ((foldingFactors?.Item1??1L) == 1L) &&
                ((foldingFactors?.Item2 ?? 1L) == 1L)
                ? estimator
                : new BitMinwiseHashEstimatorFullData
                {
                    Capacity = estimator.Capacity/(foldingFactors?.Item1??1L),
                    HashCount = estimator.HashCount,
                    BitSize = estimator.BitSize,
                    ItemCount = estimator.ItemCount,                   
                };
            res.Values = (estimator.Values == null && otherEstimator.Values == null) ? null : new int[res.Capacity];           
            if (res.Values==null) return res;
            for (var i = 0L; i < res.Capacity; i++)
            {
                var estimatorValue = GetFolded(estimator.Values, i, foldingFactors?.Item1, Math.Min, int.MaxValue);
                var otherEstimatorValue = GetFolded(otherEstimator.Values, i, foldingFactors?.Item1, Math.Min, int.MaxValue);
                res.Values[i] = Math.Min(estimatorValue, otherEstimatorValue);
            }
            res.ItemCount += otherEstimator.ItemCount;
            return res;
        }

        #region Methods
        private static BitArray CreateBitArray(IBitMinwiseHashEstimatorData estimator)
        {
            if (estimator==null)
                throw new ArgumentNullException(nameof(estimator));
            var estimatorBitArray = estimator.Values == null
                ? new BitArray((int) estimator.Capacity*estimator.BitSize*estimator.HashCount)
                : new BitArray(estimator.Values)
                {
                    Length = (int) estimator.Capacity*estimator.BitSize*estimator.HashCount
                };
            if (estimator.Values == null)
            {
                estimatorBitArray.SetAll(true);
            }
            return estimatorBitArray;
        }

        /// <summary>
        /// Compute similarity between <paramref name="minHashValues1"/> and <paramref name="minHashValues2"/>
        /// </summary>
        /// <param name="minHashValues1">The values</param>
        /// <param name="minHashValues2">The values to compare against</param>
        /// <param name="numHashFunctions">The number of hash functions to use</param>
        /// <param name="bitSize">The number of bits for a single cell</param>
        /// <returns></returns>
        /// <remarks>Handles differences in size between the bit arrays although that is not ideal and will magnify any differences.</remarks>
        private static double ComputeSimilarityFromSignatures(
            BitArray minHashValues1, 
            BitArray minHashValues2,
            int numHashFunctions, 
            byte bitSize)
        {
            if (minHashValues1 == null || 
                minHashValues2 == null ||
                numHashFunctions <= 0 ||
                bitSize == 0) return 0.0D;
            if (minHashValues1.Length > minHashValues2.Length)
            {
                //swap to ensure minHashValues1 is the smallest.
                var swap = minHashValues1;
                minHashValues1 = minHashValues2;
                minHashValues2 = swap;
            }
             uint identicalMinHashes = 0;
            var bitRange = Enumerable.Range(0, bitSize).ToArray();
            var blockSizeinBits1 = (minHashValues1.Length/numHashFunctions) * bitSize;
            var minHash1Length = minHashValues1.Length/bitSize;
             var sizeDiffInBitsPerBlock =  bitSize*((minHashValues2.Length - minHashValues1.Length)/numHashFunctions);
            var idx1 = 0;
            var idx2 = 0;
            for (var i = 0; i < minHash1Length; i++)
            {
                if (bitRange
                    .All(b => minHashValues1.Get(idx1 + b) == minHashValues2.Get(idx2 + b)))
                {
                    identicalMinHashes++;
                }
                idx1 += bitSize;
                idx2 += bitSize;
                if (sizeDiffInBitsPerBlock > 0 &&
                    idx1 % blockSizeinBits1 == 0)
                {
                    idx2 += sizeDiffInBitsPerBlock;
                }
            }
             return identicalMinHashes / (1.0D * minHashValues2.Length / bitSize);
        }

        private static T GetFolded<T>(
            T[] values, 
            long position, 
            long? foldFactor, 
            Func<T, T, T> foldOperator, 
            T defaultValue = default(T))
        {
            if (values == null) return defaultValue;
            if ((foldFactor ?? 0L) <= 1L) return values[position];
            var foldedSize = values.Length / foldFactor.Value;
            position = position % foldedSize;
            var val = values[position];
            foldFactor--;
            position += foldedSize;
            while (foldFactor > 0)
            {
                val = foldOperator(val, values[position]);
                foldFactor--;
                position += foldedSize;
            }
            return val;
        }
        #endregion
    }
}

