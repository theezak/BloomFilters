using System.Net;

namespace TBag.BloomFilters.MathExt
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
  
    /// <summary>
    /// Math extensions
    /// </summary>
    public static class MathExtensions
    {
        private static volatile Tuple<long, IEnumerable<long>> _primeCache;

        /// <summary>
        /// Calculate GCD.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></pa?ram>
        /// <returns></returns>
        public static long? GetGcd(long a, long b)
        {
            if (a == 0L || b == 0L) return null;
            var _gcd = 1L;
            a = Math.Abs(a);
            b = Math.Abs(b);
            if (a == b) { return a; }
            else if (a > b && a % b == 0L) { return b; }
            else if (b > a && b % a == 0L) { return a; }
            while (b != 0L)
            {
                _gcd = b;
                b = a % b;
                a = _gcd;
            }
            return _gcd;
        }

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
                    if (index < result.Count)
                    {
                        var bp = result[(int) index];
                        var sqr = bp*bp;
                        result.RemoveAll(i => i >= sqr && i%bp == 0);
                    }
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
        public static IEnumerable<long> GetFactors(long number)
        {
            return GetPrimeFactors(number)
                .GetPowerSet()
                .Select(factorList => factorList.Aggregate(1L, (x, y) => x*y))
                .Distinct();
        }
    }
}
