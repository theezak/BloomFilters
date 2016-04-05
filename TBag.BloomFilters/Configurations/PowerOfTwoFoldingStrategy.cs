namespace TBag.BloomFilters.Configurations
{
    using System;
    using MathExt;

    /// <summary>
    /// Folding strategies. See http://hbase.apache.org/0.94/apidocs/src-html/org/apache/hadoop/hbase/util/ByteBloomFilter.html#line.495
    /// </summary>
    public class PowerOfTwoFoldingStrategy : IFoldingStrategy
    {
        public long  ComputeFoldableSize(long size, int foldFactor)
        {
            if (foldFactor <= 0) return size;
            long byteSizeLong = size;
            unchecked
            {             
                int mask = (1 << foldFactor) - 1;
                if ((mask & byteSizeLong) != 0)
                {
                    byteSizeLong >>= foldFactor;
                    ++byteSizeLong;
                    byteSizeLong <<= foldFactor;
                }
            }
            if (byteSizeLong < 0)
            {
                throw new ArgumentException("byteSize=" + byteSizeLong + " too "
                                            + "large for bitSize=" + size + ", foldFactor=" + foldFactor);
            }
            return byteSizeLong;
        }

        /// <summary>
        /// Find a good folding factor.
        /// </summary>
        /// <param name="blockSize"></param>
        /// <param name="capacity"></param>
        /// <param name="keyCount">The actual number of keys.</param>
        /// <returns></returns>
        public uint? FindFoldFactor(long blockSize, long capacity, long? keyCount = null)
        {
            if (!keyCount.HasValue || keyCount > 0)
            {
                var pieces = 1;
                var newSize = blockSize;
                var newCapacity = capacity;
                while ((newSize & 1) == 0 && (!keyCount.HasValue || (newCapacity > keyCount.Value << 1)))
                {
                    pieces <<= 1;
                    newSize >>= 1;
                    newCapacity >>= 1;
                }
                if (pieces > 1)
                    return (uint)pieces;
            }
            return null;
        }

        public Tuple<long, long> GetFoldFactors(long size1, long size2)
        {
            var gcd = MathExtensions.GetGcd(size1, size2);
            if (!gcd.HasValue || gcd < 1) return new Tuple<long, long>(1, 1);
            return new Tuple<long, long>(size1 / gcd.Value, size2 / gcd.Value);
        }
    }
}
