namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Generate smooth numbers.
    /// </summary>
    /// <remarks>TODO: memoize the prime numbers</remarks>
    public class SmoothNumberGenerator
    {
        private static volatile Tuple<long,IEnumerable<long>> primeCache;
        

        /// <summary>
        /// See http://citeseerx.ist.psu.edu/viewdoc/download;jsessionid=096966BF3B52058BEBC90A463A806B19?doi=10.1.1.259.4308&rep=rep1&type=pdf
        /// </summary>
        /// <param name="minimum"></param>
        /// <param name="range"></param>
        /// <param name="smoothness">Determines the range of the primes used for finding smooth numbers</param>
        /// <returns></returns>
        public long[] GetSmoothNumbers(long minimum, long range, long smoothness)
        {
            var w = new long[range];
            foreach (var prime in GetPrimes(Math.Min(minimum, smoothness)))
            {
                for (var i = 0L; i < range; i++)
                {
                    var primeToPower = prime;
                    while (primeToPower <= minimum + range)
                    {
                        if ((minimum + i)%primeToPower == 0)
                        {
                            var successive = i;
                            while (successive < range)
                            {
                                w[successive] += (long) Math.Log(prime);
                                successive += primeToPower;
                            }
                        }
                        primeToPower *= prime;
                    }
                }
            }
            var logMin = Math.Log(minimum);
            return
                w.Select((r, s) => new {Crossed = r, Index = s})
                    .Where(r => r.Crossed >= logMin)
                    .OrderByDescending(r=>r.Crossed)
                    .Select(r => minimum + r.Index)
                    .ToArray();
        }

       internal static IEnumerable<long> GetPrimes(long to)
        {
            var cached = primeCache;
            if (cached != null && cached.Item1 >= to)
            {
                return cached.Item2.Where(p => p <= to).ToArray();
            }
            var from = 0L;
            var max = (long)Math.Floor(2.52 * Math.Sqrt(to) / Math.Log(to));
            var res = LongEnumerable.Range(from, max).Aggregate(
                LongEnumerable.Range(2, to - 1).ToList(),
                (result, index) =>
                {
                    var bp = result[(int) index];
                    var sqr = bp*bp;
                    result.RemoveAll(i => i >= sqr && i%bp == 0);
                    return result;
                });
            primeCache = new Tuple<long, IEnumerable<long>>(to, res);
            return res;
        }
    }
}
