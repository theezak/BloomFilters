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
        /// <param name="a">First number</param>
        /// <param name="b">Second number</param>
        /// <returns>GCD of <paramref name="a"/> and <paramref name="b"/>.</returns>
        public static long? GetGcd(long a, long b)
        {
            if (a == 0L || b == 0L) return null;
            var gcd = 1L;
            a = Math.Abs(a);
            b = Math.Abs(b);
            if (a == b) { return a; }
            if (a > b && a % b == 0L) { return b; }
            if (b > a && b % a == 0L) { return a; }
            while (b != 0L)
            {
                gcd = b;
                b = a % b;
                a = gcd;
            }
            return gcd;
        }

        /// <summary>
        /// Get the primes
        /// </summary>
        /// <param name="to"></param>
        /// <returns>All primes up to <paramref name="to"/>.</returns>
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
        /// Get the prime factors for <paramref name="number"/>.
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
        /// Get all factors for <paramref name="number"/>.
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
