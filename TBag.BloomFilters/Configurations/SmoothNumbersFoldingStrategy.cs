namespace TBag.BloomFilters.Configurations
{
    using System.Collections.Generic;

    /// <summary>
    /// Folding strategy based upon smooth numbers.
    /// </summary>
    /// <remarks>Underlying thought is that smooth numbers are the most composite numbers that are not sparse (highly compisite and largely composite numbers tend to be too sparse).</remarks>
    public class SmoothNumbersFoldingStrategy : IFoldingStrategy
    {
        private readonly SmoothNumberGenerator _smoothNumberGenerator = new SmoothNumberGenerator();
        private const byte MaxTrials = 5;

        /// <summary>
        /// Compute a foldable size of at least <paramref name="blockSize"/>.
        /// </summary>
        /// <param name="blockSize">The Bloom filter size.</param>
        /// <param name="foldFactor">The fold factor desired</param>
        /// <returns></returns>
        /// <remarks>Ignores the desired fold factor.</remarks>
        public long ComputeFoldableSize(long blockSize, int foldFactor)
        {
            var trials = MaxTrials;
            var smoothNumbers = default(long[]);
            var smoothness = 2000;
            while (trials > 0 && (smoothNumbers == null || smoothNumbers.Length == 0))
            {
                smoothNumbers = _smoothNumberGenerator.GetSmoothNumbers(blockSize, 200, smoothness);
                trials--;
                blockSize += 200;
                smoothness += 2000;
            }
            return smoothNumbers!=null && smoothNumbers.Length > 0 ? smoothNumbers[0] : blockSize;
        }

        /// <summary>
        /// Find a fold factor.
        /// </summary>
        /// <param name="blockSize">The size of the Bloom filter</param>
        /// <param name="hashFunctionCount"></param>
        /// <param name="keyCount">The number of keys added to the Bloom filter. When not provided, the fold advice will not take the error rate into consideration and provide a maximal fold given the capacity.</param>
        /// <returns>A fold factor.</returns>
        public uint? FindFoldFactor(long blockSize, long capacity, long? keyCount = null)
        {
            if (!keyCount.HasValue || keyCount > 0)
            {
                var remaining = blockSize;
                var factors = new List<long>();
                foreach (var prime in SmoothNumberGenerator.GetPrimes(blockSize))
                {
                    while (remaining > 1 && remaining%prime == 0)
                    {
                        remaining = remaining/prime;
                        factors.Add(prime);
                    }
                    if (remaining <= 1) break;
                }
                uint pieces = 1;
                var newSize = blockSize;
                var newCapacity = capacity;
                foreach (var factor in factors)
                {
                    if (newSize/factor > 1 &&
                        (!keyCount.HasValue || newCapacity/factor > keyCount.Value) &&
                        pieces < blockSize)
                    {
                        pieces = (uint) (pieces*factor);
                        newSize = newSize/factor;
                        newCapacity = newCapacity/factor;
                    }
                    else
                    {
                        break;
                    }
                }
                if (pieces > 1) return pieces;
            }
            return null;
        }
    }
}
