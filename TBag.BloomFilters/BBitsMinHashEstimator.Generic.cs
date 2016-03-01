namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Collections;
    using System.Diagnostics;


    public static class BitArrayExtensions
    {
        // <summary>
        // serialize a bitarray.
        // </summary>
        //<param name="bits"></param>
        // <returns></returns>
        public static IEnumerable<byte> ToBytes(this BitArray bits)
        {
            // calculate the number of bytes
            int numBytes = bits.Count/8;
            // add an extra byte if the bit-count is not divisible by 8
            if (bits.Count%8 != 0) numBytes++;
            // reserve the correct number of bytes
            byte[] bytes = new byte[numBytes];
            // get the 4 bytes that make up the 32 bit integer of the bitcount
            var prefix = BitConverter.GetBytes(bits.Count);
            // copy the bit-array into the byte array
            bits.CopyTo(bytes, 0);
            // read off the prefix
            foreach (var b in prefix)
                yield return b;
            // read off the body
            foreach (var b in bytes)
                yield return b;
        }


        /// <summary>
        /// restore a BitArray from the enumeration of bytes
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static BitArray ToBitArray(this IEnumerable<byte> bytes)
        {
            // take the first 4 bytes and restore a 32 bit integer indicating the bit-length
            int numBits = BitConverter.ToInt32(bytes.Take(4).ToArray(), 0);
            // skipping the 4 leader bytes, restore the bitarray
            var ba = new BitArray(bytes.Skip(4).ToArray());
            // set the length exactly
            ba.Length = numBits;
            // return the bit-array;
            return ba;
        }
    }

    /// <summary>
    /// A b-bits min hash estimator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BBitsMinHashEstimator<T>
    {
        private readonly int _hashCount;

        private delegate int Hash(T item);

        private readonly Hash[] m_hashFunctions;
        private BitArray _hashValues;
        private readonly byte _bitSize;
        private readonly Func<T, int> _entityMap;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="universeSize">The range for hash values</param>
        /// <param name="entityMap">Map an entity to a numeric identifier/value</param>
        /// <param name="bitSize">The number of bits to store per hash</param>
        /// <param name="hashCount">The number of hash functions to use.</param>
        /// <remarks>By using bitSize = 1 or bitSize = 2, the accuracy is decreased, thus the hashCount needs to be increased. However, when resemblance is not too small, for example > 0.5, bitSize = 1 can yield similar results as bitSize = 64 with only 3 times the hash count.</remarks>
        public BBitsMinHashEstimator(int universeSize, Func<T, int> entityMap, byte bitSize, int hashCount)
        {
            Debug.Assert(universeSize > 0);
            _bitSize = bitSize;
            _hashCount = hashCount;
            m_hashFunctions = new Hash[_hashCount];
            _entityMap = entityMap;
            Random r = new Random(11);
            for (int i = 0; i < _hashCount; i++)
            {
                uint a = (uint) r.Next(universeSize);
                uint b = (uint) r.Next(universeSize);
                uint c = (uint) r.Next(universeSize);
                m_hashFunctions[i] = itm => QHash(_entityMap(itm), a, b, c, (uint) universeSize);

            }
        }

        public double Similarity(BBitsMinHashEstimator<T> set2)
        {
            if (set2 == null ||
                set2._bitSize != _bitSize ||
                set2.m_hashFunctions == null ||
                m_hashFunctions == null ||
                set2.m_hashFunctions.Length != m_hashFunctions.Length) return 0.0D;
            return ComputeSimilarityFromSignatures(_hashValues, set2._hashValues, _hashCount, _bitSize);
        }


        public void Add(HashSet<T> set1)
        {
            Debug.Assert(set1.Count > 0);
            var slots = GetMinHashSlots(_hashCount, set1.Count);
            ComputeMinHashForSet(slots, set1);
            Convert(slots);
        }

        private void Convert(int[,] slots)
        {
            _hashValues = new BitArray(_bitSize*slots.GetLength(0)*slots.GetLength(1));
            for (var hashCount = 0; hashCount < slots.GetLength(0); hashCount++)
            {
                for (var eltCount = 0; eltCount < slots.GetLength(1); eltCount++)
                {
                    var byteValue = BitConverter.GetBytes(slots[hashCount, eltCount]).First();
                    var idx = hashCount*eltCount*_bitSize;
                    for (int b = 0; b < _bitSize; b++)
                    {
                        _hashValues.Set(idx + b, (byteValue & (1 << b - 1)) != 0);
                    }
                }
            }
        }

        private void ComputeMinHashForSet(int[,] minHashValues, HashSet<T> set1)
        {
            var idx = 0;
            foreach (T element in set1)
            {
                for (int i = 0; i < _hashCount; i++)
                {
                    int hindex = m_hashFunctions[i](element);
                    if (hindex < minHashValues[i, idx])
                    {
                        minHashValues[i, idx] = hindex;
                    }
                }
                idx++;
            }
        }

        private static int[,] GetMinHashSlots(int numHashFunctions, int setSize)
        {
            var minHashValues = new int[numHashFunctions, setSize];
            for (int i = 0; i < numHashFunctions; i++)
                for (int j = 0; j < numHashFunctions; j++)
                {
                    minHashValues[i, j] = Int32.MaxValue;
                }
            return minHashValues;
        }

        private static int QHash(int id, uint a, uint b, uint c, uint bound)
        {

            //Modify the hash family as per the size of possible elements in a Set
            int hashValue = (int) ((a*(id >> 4) + b*id + c) & 131071);
            return (int) (Math.Abs(hashValue)%bound);
        }



        private static double ComputeSimilarityFromSignatures(BitArray minHashValues1, BitArray minHashValues2,
            int numHashFunctions, byte bitSize)
        {
            int identicalMinHashes = 0;
            var unions = numHashFunctions;
            if (minHashValues1 != null && minHashValues2 != null)
            {
                var range = Enumerable.Range(0, bitSize).ToArray();
                var minHash1Length = minHashValues1.Count/(bitSize*numHashFunctions);
                var minHash2Length = minHashValues2.Count/(bitSize*numHashFunctions);
                unions = numHashFunctions*minHash1Length*minHash2Length;
                for (int i = 0; i < numHashFunctions; i++)
                {
                    for (int iMinHash2 = 0; iMinHash2 < minHash2Length; iMinHash2++)
                    {
                        for (int iMinHash1 = 0; iMinHash1 < minHash1Length; iMinHash1++)
                        {
                            var minHash1Idx = numHashFunctions*bitSize*iMinHash1;
                            var minHash2Idx = numHashFunctions*bitSize*iMinHash1;
                            if (
                                range.All(
                                    b => minHashValues1.Get(minHash1Idx + b) == minHashValues2.Get(minHash2Idx + b)))
                            {
                                identicalMinHashes++;
                            }
                        }
                    }
                }
            }
            return (1.0*identicalMinHashes)/unions;
        }

    }
}
