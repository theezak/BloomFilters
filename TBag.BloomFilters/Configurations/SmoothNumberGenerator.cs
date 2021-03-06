﻿namespace TBag.BloomFilters.Configurations
{
    using System;
    using System.Linq;
    using MathExt;

    /// <summary>
    /// Generate smooth numbers.
    /// </summary>
   internal class SmoothNumberGenerator
    {
     
        /// <summary>
        /// Get all smooth numbers in the given range.
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
