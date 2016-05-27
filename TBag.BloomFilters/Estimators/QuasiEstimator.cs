


namespace TBag.BloomFilters.Estimators
{
    using Configurations;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    /// <summary>
    /// Decoding algorithm for quasi estimation.
    /// </summary>
    /// <remarks>Quasi estimation uses one Bloom filter (representing the first set) and a set of items (representing the second set) to estimate the number of differences between the two sets.</remarks>
    internal static class QuasiEstimator
    {
        /// <summary>
        /// Get the ideal error rate and adjustment factor function.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="blockSize"></param>
        /// <param name="itemCount"></param>
        /// <param name="hashFunctionCount"></param>
        /// <param name="errorRate"></param>
        /// <returns></returns>
        internal static Tuple<float, Func<long, long, long>> GetAdjustmentFactor(
            IBloomFilterSizeConfiguration configuration, 
            long blockSize,  
            long itemCount, 
            uint hashFunctionCount, 
            float errorRate)
        {
            var idealBlockSize = configuration.BestCompressedSize(
            itemCount,
            errorRate);
            var idealErrorRate = configuration.ActualErrorRate(
                idealBlockSize,
                itemCount,
                hashFunctionCount);
            var actualErrorRate = Math.Max(
                idealErrorRate,
                configuration.ActualErrorRate(
                    blockSize,
                    itemCount,
                    hashFunctionCount));
            var factor = (actualErrorRate - idealErrorRate);
            if (actualErrorRate >= 0.9D &&
                blockSize > 0)
            {
                //arbitrary. Should really figure out what is behind this one day : - ). What happens is that the estimator has an extremely high
                //false-positive rate. Which is the reason why this approach is not ideal to begin with. 
                factor = 2 * factor * ((float)idealBlockSize / blockSize);
            }
            return new Tuple<float, Func<long, long, long>>(
                idealErrorRate, 
                (membershipCount, sampleCount) => (long)Math.Floor(membershipCount - factor * (sampleCount - membershipCount)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="setSize">The total item count for the Bloom filter</param>
        /// <param name="errorRate">Error rate of the Bloom filter</param>
        /// <param name="membershipTest">Test membership in the Bloom filter</param>
        /// <param name="otherSetSample">A set of items to test against the Bloom filter.</param>
        /// <param name="otherSetSize">Optional total size. When not given, the sample size will be used as the total size. When the total size does not match the sample set, the difference will be proportionally scaled.</param>
        /// <param name="membershipCountAdjuster">Optional function to adjust the membership count based upon membership count and sample count (size of <paramref name="otherSetSample"/>.</param>
        /// <returns>The estimated number of differences between the two sets, or <c>null</c> when a reasonable estimate can't be given.</returns>
        /// <remarks>When the <paramref name="otherSetSize"/> is given and does not equal the number of items in <paramref name="otherSetSample"/>, the assumption is that <paramref name="otherSetSample"/> is a random, representative, sample of the total set.</remarks>
        internal static long? Decode<TEntity>(
            long setSize,
            double errorRate,
            Func<TEntity, bool> membershipTest,
            IEnumerable<TEntity> otherSetSample,
            long? otherSetSize = null,
            Func<long, long, long> membershipCountAdjuster = null
            )
        {
            if (otherSetSample == null) return setSize;
            if (setSize == 0L && otherSetSize.HasValue) return otherSetSize.Value;
            var membershipCount = 0L;
            Array r;
            var samples = (otherSetSample is IList<TEntity>) ? (IList<TEntity>)otherSetSample :otherSetSample.ToArray();
            Parallel.ForEach(
                       Partitioner.Create(0, samples.Count),
                       (range, state) =>

                       {
                           var rangeMemberCount = 0L;
                           for (int i = range.Item1; i < range.Item2; i++)
                           {
                               if (membershipTest(samples[i]))

                                   rangeMemberCount++;
                           }
                           Interlocked.Add(ref membershipCount, rangeMemberCount);
                       });
            if (samples.Count == 0) return setSize;
            if (setSize == 0L) return samples.Count;
            if (otherSetSize.HasValue &&
               otherSetSize.Value != samples.Count)
            {
                membershipCount = (long)Math.Ceiling(membershipCount * ((double)otherSetSize.Value / Math.Max(1, samples.Count)));
            }
            if (samples.Count == membershipCount && 
                    setSize != (otherSetSize ?? samples.Count))
            {
                //Obviously there is a difference, but we didn't find one (each item was a member): do the best we can with the set sizes.
                //assume the difference in set size is the major contributor (since we didn't detect many differences in value).
                membershipCount = (otherSetSize ?? samples.Count) == 0L
                    ? 0L
                    : (long)
                        (membershipCount*
                         ((double) Math.Min(setSize, otherSetSize ?? samples.Count)/
                          Math.Max(otherSetSize ?? samples.Count, setSize)));
            }        
            if (membershipCountAdjuster != null)
            {
                membershipCount = membershipCountAdjuster(membershipCount, otherSetSize ?? samples.Count);
            }
            if (membershipCount < 0)
            {
                membershipCount = 0;
            }
            //membership count can't exceed the set size.
            membershipCount = Math.Min(membershipCount, setSize);
            otherSetSize = otherSetSize ?? samples.Count;
            var difference = setSize - otherSetSize.Value +
                             2 * (otherSetSize.Value - membershipCount) / (1 - errorRate);
            //difference is capped by the count of all items.
            return Math.Min((long)Math.Ceiling(Math.Abs(difference)), setSize + otherSetSize.Value);
        }
    }
}
