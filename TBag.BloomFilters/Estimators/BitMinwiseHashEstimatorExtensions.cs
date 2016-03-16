namespace TBag.BloomFilters.Estimators
{
    using System;
    using System.Collections;
    using System.Linq;

    /// <summary>
    /// Extension methods for bit minwise hash estimator data
    /// </summary>
    public static class BitMinwiseHashEstimatorExtensions
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
            return ComputeSimilarityFromSignatures(
                new BitArray(estimator.Values)
                {
                    Length = (int)estimator.Capacity * estimator.BitSize * estimator.HashCount
                },
                new BitArray(otherEstimatorData.Values)
                {
                    Length = (int)otherEstimatorData.Capacity * otherEstimatorData.BitSize * otherEstimatorData.HashCount
                },
                estimator.HashCount,
                estimator.BitSize);
        }

        /// <summary>
        /// Compute similarity between <paramref name="minHashValues1"/> and <paramref name="minHashValues2"/>
        /// </summary>
        /// <param name="minHashValues1">The values</param>
        /// <param name="minHashValues2">The values to compare against</param>
        /// <param name="numHashFunctions">The number of hash functions to use</param>
        /// <param name="bitSize">The number of bits for a single cell</param>
        /// <returns></returns>
        private static double ComputeSimilarityFromSignatures(
            BitArray minHashValues1, 
            BitArray minHashValues2,
            int numHashFunctions, 
            byte bitSize)
        {
            uint identicalMinHashes = 0;
            var unions = (long) numHashFunctions;
            if (minHashValues1 == null || minHashValues2 == null) return 1.0D*identicalMinHashes/unions;
            var bitRange = Enumerable.Range(0, bitSize).ToArray();
            var minHash1Length = minHashValues1.Length/bitSize;
            var minHash2Length = minHashValues2.Length/bitSize;
            var minCount = Math.Min(minHash1Length, minHash2Length);
            var idx = 0;
            for (int i = 0; i < minCount; i++)
            {
                if (bitRange
                    .All(b => minHashValues1.Get(idx + b) == minHashValues2.Get(idx + b)))
                {
                    identicalMinHashes++;
                }
                idx += bitSize;
            }
            return 1.0D*identicalMinHashes / Math.Max(minHash1Length, minHash2Length);
        }
    }
}

