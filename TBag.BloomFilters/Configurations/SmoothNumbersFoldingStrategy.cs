namespace TBag.BloomFilters.Configurations
{
    using System.Linq;
    using System;
    using MathExt;

    /// <summary>
    /// Folding strategy based upon smooth numbers.
    /// </summary>
    /// <remarks>Underlying thought is that smooth numbers are the most composite numbers that are not sparse (highly compisite and largely composite numbers tend to be too sparse).</remarks>
    public class SmoothNumbersFoldingStrategy : IFoldingStrategy
    {
        private readonly SmoothNumberGenerator _smoothNumberGenerator = new SmoothNumberGenerator();
        private const byte MaxTrials = 5;
        private static readonly double PrimePowFactor = 1.0D / (4.0D *Math.Sqrt(Math.E)) + 0.001D;
        /// <summary>
        /// Compute a foldable size of at least <paramref name="blockSize"/>.
        /// </summary>
        /// <param name="blockSize">The Bloom filter size.</param>
        /// <param name="foldFactor">The fold factor desired</param>
        /// <returns></returns>
        /// <remarks>See http://people.math.gatech.edu/~ecroot/smooth6.pdf : there are >> sqrt(x) of y smooth numbers in [x, x+sqrt(x)] for y = x^(1/(4*sqrt(e))+epsilon)</remarks>
        public long ComputeFoldableSize(long blockSize, int foldFactor)
        {
            var trials = MaxTrials;
            var trialSize = Math.Max(200L, (long) (Math.Sqrt(blockSize)/trials));
            var smoothNumbers = default(long[]);
            var smoothness = (long)Math.Max(500L, Math.Pow(blockSize, PrimePowFactor));
            while (trials > 0 && 
                (smoothNumbers == null || smoothNumbers.Length == 0))
            {
                smoothNumbers = _smoothNumberGenerator.GetSmoothNumbers(blockSize, trialSize, smoothness);
                trials--;
                blockSize += trialSize;
            }
            return smoothNumbers != null && smoothNumbers.Length > 0 ? 
                smoothNumbers.FirstOrDefault(s=> foldFactor <= 0 || s%foldFactor==0) : 
                blockSize;
        }

        /// <summary>
        /// Find a fold factor.
        /// </summary>
        /// <param name="blockSize">The size of the Bloom filter</param>
        /// <param name="capacity"></param>
        /// <param name="keyCount">The number of keys added to the Bloom filter. When not provided, the fold advice will not take the error rate into consideration and provide a maximal fold given the capacity.</param>
        /// <returns>A fold factor.</returns>
        public uint? FindFoldFactor(long blockSize, long capacity, long? keyCount = null)
        {
            if (keyCount.HasValue && !(keyCount > 0)) return null;
            var pieces = MathExtensions.GetFactors(blockSize)               
                .Where(factor => blockSize / factor > 1 &&
                                 (!keyCount.HasValue || capacity / factor > keyCount.Value) &&
                                 factor < blockSize)
                .DefaultIfEmpty()
                .Max();
            return pieces > 1 ? (uint?) (uint) pieces : null;
        }

        /// <summary>
        /// Get fold factors for the two given sizes.
        /// </summary>
        /// <param name="size1"></param>
        /// <param name="size2"></param>
        /// <returns></returns>
        public Tuple<long, long> GetFoldFactors(long size1, long size2)
        {
            var gcd = MathExtensions.GetGcd(size1, size2);
            if (!gcd.HasValue || gcd < 1) return new Tuple<long, long>(1, 1);
            return new Tuple<long, long>(size1 / gcd.Value, size2 / gcd.Value);
        }

    }
}
