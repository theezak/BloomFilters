
namespace TBag.BloomFilters.MathExt
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Collections;

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
            a = Math.Abs(a);
            b = Math.Abs(b);
            if (a == b || (b > a && b % a == 0L)) { return a; }
            if (a > b && a % b == 0L) { return b; }
            var gcd = 1L;
            while (b != 0L)
            {
                gcd = b;
                b = a % b;
                a = gcd;
            }
            return gcd;
        }


        /// <summary>
        /// Get all the primes at most equal to <param name="to"></param>
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
            var sieveContainer = new FastBitArray((int) to + 1, true);
            int marker = 2; //start
            sieveContainer[0] = false; //0 is not prime
            sieveContainer[1] = false; //1 is not prime
            while (marker * marker <= sieveContainer.Length)
            {
                var factor = marker;
                while ((factor += marker) <= to)
                {
                    sieveContainer[factor] = false;
                }
                while (!sieveContainer.Get(++marker))
                {
                }
            }
            var primes = new List<long>();
            for (var i = 0; i < sieveContainer.Length; i++)
            {
                if (sieveContainer[i])
                {
                    primes.Add(i);
                }
            }
            _primeCache = new Tuple<long, IEnumerable<long>>(to, primes);
            return primes;
        }

        /// <summary>
        /// Get the prime factors for <paramref name="number"/>.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private static List<long> GetPrimeFactors(long number)
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
