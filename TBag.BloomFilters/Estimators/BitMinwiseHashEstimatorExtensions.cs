namespace TBag.BloomFilters.Estimators
{
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
    }
}

