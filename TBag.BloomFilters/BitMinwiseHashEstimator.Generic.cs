namespace TBag.BloomFilters
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    /// <summary>
    /// A b-bits min hash estimator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BitMinwiseHashEstimator<T>
    {
        #region Fields
        private readonly int _hashCount;
        private delegate int Hash(int item);
        private readonly Hash[] m_hashFunctions;
        private BitArray _hashValues;
        private readonly byte _bitSize;
        private readonly Func<T, int> _entityHash;
        private readonly Func<T, int> _idHash;
        private readonly uint _capacity;
        private long _elementCount;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="universeSize">The range for hash values</param>
        /// <param name="entityMap">Map an entity to a numeric identifier/value</param>
        /// <param name="bitSize">The number of bits to store per hash</param>
        /// <param name="hashCount">The number of hash functions to use.</param>
        /// <remarks>By using bitSize = 1 or bitSize = 2, the accuracy is decreased, thus the hashCount needs to be increased. However, when resemblance is not too small, for example > 0.5, bitSize = 1 can yield similar results as bitSize = 64 with only 3 times the hash count.</remarks>
        public BitMinwiseHashEstimator(int universeSize, Func<T, int> entityMap, Func<T,int> idMap, 
            byte bitSize, int hashCount, uint capacity)
        {
            Debug.Assert(universeSize > 0);
            _bitSize = bitSize;
            _hashCount = hashCount;
            m_hashFunctions = new Hash[_hashCount];
            _entityHash = entityMap;
            _idHash = idMap;
            _capacity = capacity;
            Random r = new Random(11);
            for (int i = 0; i < _hashCount; i++)
            {
                uint a = (uint)r.Next(universeSize);
                uint b = (uint)r.Next(universeSize);
                uint c = (uint)r.Next(universeSize);
                m_hashFunctions[i] = itm => QHash(itm, a, b, c, (uint)universeSize);

            }
        }
        #endregion

        #region Implementation of estimator
        /// <summary>
        /// Determine similarity.
        /// </summary>
        /// <param name="set2"></param>
        /// <returns></returns>
        /// <remarks>Zero is no similarity, one is completely similar.</remarks>
        public double Similarity(BitMinwiseHashEstimator<T> set2)
        {
            if (set2 == null ||
                set2._bitSize != _bitSize ||
                set2.m_hashFunctions == null ||
                m_hashFunctions == null ||
                set2.m_hashFunctions.Length != m_hashFunctions.Length) return 0.0D;
            return ComputeSimilarityFromSignatures(_hashValues, set2._hashValues, _hashCount, _bitSize,
                Math.Abs(_elementCount - set2._elementCount));
        }

        /// <summary>
        /// Add the set to estimator.
        /// </summary>
        /// <param name="set1"></param>
        public void Add(HashSet<T> set1)
        {
            Debug.Assert(set1.Count > 0);
            var slots = GetMinHashSlots(_hashCount, _capacity);
            ComputeMinHashForSet(slots, set1);
            _elementCount += set1.LongCount();
            Convert(slots);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Convert array to bit array.
        /// </summary>
        /// <param name="slots"></param>
        private void Convert(int[,] slots)
        {
            _hashValues = new BitArray(_bitSize * slots.GetLength(0) * slots.GetLength(1));
            var valueCount = slots.GetLength(1);
            var blockSize = valueCount * _bitSize;
            for (var hashCount = 0; hashCount < slots.GetLength(0); hashCount++)
            {
                for (var eltCount = 0; eltCount < valueCount ; eltCount++)
                {
                    var byteValue = BitConverter.GetBytes(slots[hashCount, eltCount]);
                    var byteValueIdx = 0;
                    var idx = (hashCount * blockSize)+(eltCount* _bitSize);
                    for (int b = 0; b < _bitSize; b++)
                    {
                        _hashValues.Set(idx + b, (byteValue[byteValueIdx] & (1 << (b%8))) != 0);
                        if (b > 0 && b % 8 == 0)
                        {
                            byteValueIdx++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Compute the hash for the given set.
        /// </summary>
        /// <param name="minHashValues"></param>
        /// <param name="set1"></param>
        private void ComputeMinHashForSet(int[,] minHashValues, HashSet<T> set1)
        {
            foreach(var element in set1)
             {
                 var idHash = _idHash(element)%_capacity;
                var entityHash = _entityHash(element);
                 for (int i = 0; i < _hashCount; i++)
                 {
                     int hindex = m_hashFunctions[i](entityHash);
                     if (hindex < minHashValues[i, idHash])
                     {
                         minHashValues[i, idHash] = hindex;
                     }
                 }
             };
        }

        /// <summary>
        /// Create an array of the correct size.
        /// </summary>
        /// <param name="numHashFunctions"></param>
        /// <param name="setSize"></param>
        /// <returns></returns>
        private static int[,] GetMinHashSlots(int numHashFunctions, uint setSize)
        {
            var minHashValues = new int[numHashFunctions, setSize];
            Enumerable
                .Range(0, numHashFunctions - 1)
                .ToArray()
                .AsParallel()
                .ForAll(i =>
              {
                  for (var j = 0; j < setSize; j++)
                  {
                      minHashValues[i, j] = Int32.MaxValue;
                  }
              });
            return minHashValues;
        }

        /// <summary>
        /// QHash.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="bound"></param>
        /// <returns></returns>
        private static int QHash(int id, uint a, uint b, uint c, uint bound)
        {

            //Modify the hash family as per the size of possible elements in a Set
            int hashValue = (int)((a * (id >> 4) + b * id + c) & 131071);
            return (int)(Math.Abs(hashValue) % bound);
        }


        /// <summary>
        /// Compute similarity.
        /// </summary>
        /// <param name="minHashValues1"></param>
        /// <param name="minHashValues2"></param>
        /// <param name="numHashFunctions"></param>
        /// <param name="bitSize"></param>
        /// <returns></returns>
        private static double ComputeSimilarityFromSignatures(BitArray minHashValues1, BitArray minHashValues2,
            int numHashFunctions, byte bitSize, long elementCountDiff)
        {
            int identicalMinHashes = 0;
            var unions = (long)numHashFunctions;
            if (minHashValues1 != null && minHashValues2 != null)
            {
                var bitRange = Enumerable.Range(0, bitSize).ToArray();
                var minHash1Length = minHashValues1.Count / (bitSize * numHashFunctions);
                var minHash2Length = minHashValues2.Count / (bitSize * numHashFunctions);
                var count = Math.Min(minHash1Length, minHash2Length);
                var blockSize = count * bitSize;
                unions = numHashFunctions * Math.Max(minHash1Length, minHash2Length) + elementCountDiff;
                for (int i = 0; i < numHashFunctions; i++)
                {
                    for (int j = 0; j < count; j++)
                    {
                        var idx = (i * blockSize) + (bitSize * j);
                        if (bitRange
                            .All(b => minHashValues1.Get(idx + b) == minHashValues2.Get(idx + b)))
                        {
                            identicalMinHashes++;
                        }
                    }
                }
            }
            return (1.0D * identicalMinHashes) / unions;
        }
        #endregion
    }
}
