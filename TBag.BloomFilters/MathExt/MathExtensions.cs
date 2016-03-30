

using System;
using System.Collections.Generic;
using System.Linq;
using TBag.BloomFilters.Collections.Generics;

namespace TBag.BloomFilters.MathExt
{
    internal static class MathExtensions
    {
        private static volatile Tuple<long, IEnumerable<long>> _primeCache;

        /// <summary>
        /// Get the primes
        /// </summary>
        /// <param name="to"></param>
        /// <returns></returns>
        internal static IEnumerable<long> GetPrimes(long to)
        {
            var cached = _primeCache;
            if (cached != null && cached.Item1 >= to)
            {
                return cached.Item2.Where(p => p <= to).ToArray();
            }
            var max = (long)Math.Floor(2.52 * Math.Sqrt(to) / Math.Log(to));
            var res = LongEnumerable.Range(0L, max).Aggregate(
                LongEnumerable.Range(2, to - 1).ToList(),
                (result, index) =>
                {
                    var bp = result[(int)index];
                    var sqr = bp * bp;
                    result.RemoveAll(i => i >= sqr && i % bp == 0);
                    return result;
                });
            _primeCache = new Tuple<long, IEnumerable<long>>(to, res);
            return res;
        }

        /// <summary>
        /// Get the prime factors
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        internal static List<long> GetPrimeFactors(long number)
        {
            var factors = new List<long>();
            foreach (var prime in GetPrimes(number))
            {
                while (number > 1 && number % prime == 0)
                {
                    number = number / prime;
                    factors.Add(prime);
                }
                if (number <= 1) break;
            }
            return factors;
        }

        /// <summary>
        /// Get all factors
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        internal static IEnumerable<long> GetFactors(long number)
        {
            return GetPrimeFactors(number)
                .GetPowerSet()
                .Select(factorList => factorList.Aggregate(1L, (x, y) => x*y))
                .Distinct();
        }
    }
}
