using TBag.BloomFilters.MathExt;

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
            foreach (var prime in MathExtensions.GetPrimes(Math.Min(minimum, smoothness)))
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
                    .GroupBy(r=>r.Crossed)
                    .OrderByDescending(r=>r.Key)                    
                    .SelectMany(grp => grp.Select(r => minimum + r.Index).OrderBy(smooth=>smooth))
                    .ToArray();
        }     
    }
}
