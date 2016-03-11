using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBag.BloomFilter.Test
{
    internal static class MathExtensions
    {
        public static double Variance(this IEnumerable<int> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            int n = 0;
            double mean = 0;
            double M2 = 0;

            foreach (var x in source)
            {
                n++;

                double delta = (double)x - mean;
                mean += delta / n;
                M2 += delta * ((double)x - mean);
            }

            if (n < 2)
                throw new InvalidOperationException("Source must have at least 2 elements");

            return (double)(M2 / (n - 1));

        }
    }
}
