namespace TBag.BloomFilters.Measurements.Test
{
    using System;
    using System.Collections.Generic;
   
    internal static class MathExtensions
    {
        public static double Variance(this IEnumerable<int> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var n = 0;
            var mean = 0.0D;
            var m2 = 0.0D;

            foreach (var x in source)
            {
                n++;

                var delta = (double)x - mean;
                mean += delta / n;
                m2 += delta * ((double)x - mean);
            }

            if (n < 2)
                throw new InvalidOperationException("Source must have at least 2 elements");

            return (double)(m2 / (n - 1));

        }
    }
}
