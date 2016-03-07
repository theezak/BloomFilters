namespace TBag.BloomFilters
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Serialization;

    /// <summary>
    /// A b-bits min hash estimator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// /// <typeparam name="TId"></typeparam>
    public class BitMinwiseHashEstimator<T,TId>
    {
        #region Fields
        private readonly int _hashCount;
        private readonly Func<T, IEnumerable<int>> _hashFunctions;
        private readonly Func<TId, long> _idHash;
        private BitArray _hashValues;
        private readonly byte _bitSize;
        private readonly IBloomFilterConfiguration<T, int, TId,long> _configuration;
        private readonly ulong _capacity;
        private int[,] _slots;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="entityMap">Map an entity to a numeric identifier/value</param>
        /// <param name="bitSize">The number of bits to store per hash</param>
        /// <param name="hashCount">The number of hash functions to use.</param>
        /// <remarks>By using bitSize = 1 or bitSize = 2, the accuracy is decreased, thus the hashCount needs to be increased. However, when resemblance is not too small, for example > 0.5, bitSize = 1 can yield similar results as bitSize = 64 with only 3 times the hash count.</remarks>
        public BitMinwiseHashEstimator(IBloomFilterConfiguration<T,int,TId,long> configuration,
            byte bitSize, int hashCount, ulong capacity)
        {
            _bitSize = bitSize;
            _capacity = capacity;
            _hashCount = hashCount;
            _configuration = configuration;
            _hashFunctions = GenerateHashes();
            _idHash = id => (Math.Abs(configuration.IdHashes(id, 1).First()))% (long)_capacity;
            _slots = GetMinHashSlots(_hashCount, _capacity);
        }
        #endregion

        #region Implementation of estimator
        /// <summary>
        /// Determine similarity.
        /// </summary>
        /// <param name="set2"></param>
        /// <returns></returns>
        /// <remarks>Zero is no similarity, one is completely similar.</remarks>
        public double Similarity(BitMinwiseHashEstimator<T,TId> set2)
        {
            Convert();
            if (set2 == null ||
                set2._bitSize != _bitSize) return 0.0D;
            set2.Convert();
            return ComputeSimilarityFromSignatures(_hashValues, set2._hashValues, _hashCount, _bitSize);
        }

        /// <summary>
        /// Add the set to estimator.
        /// </summary>
        /// <param name="set1"></param>
        public void Add(T item)
        {
            Debug.Assert(item != null);
            ComputeMinHash(item);
        }

        public BitMinwiseHashEstimatorData Extract()
        {
            return new BitMinwiseHashEstimatorData
            {
                 BitSize = _bitSize,
                 Capacity = _capacity,
                  HashCount = _hashCount,
                  Values = _hashValues.ToBytes().ToArray()
            };
        }
        #endregion

        #region Methods
        /// <summary>
        /// Convert array to bit array.
        /// </summary>
        private void Convert()
        {
            _hashValues = new BitArray(_bitSize * _slots.GetLength(0) * _slots.GetLength(1));
            var valueCount = _slots.GetLength(1);
            var idx = 0;
            for (var hashCount = 0; hashCount < _slots.GetLength(0); hashCount++)
            {
                for (var eltCount = 0; eltCount < valueCount ; eltCount++)
                {
                    var byteValue = BitConverter.GetBytes(_slots[hashCount, eltCount]);
                    var byteValueIdx = 0;
                    for (int b = 0; b < _bitSize; b++)
                    {
                        _hashValues.Set(idx + b, (byteValue[byteValueIdx] & (1 << (b%8))) != 0);
                        if (b > 0 && b % 8 == 0)
                        {
                            byteValueIdx = (byteValueIdx+1)%byteValue.Length;
                        }
                    }
                    idx += _bitSize;
                }
            }
        }

        private Func<T,IEnumerable<int>> GenerateHashes()
        {
            var universeSize = int.MaxValue;
            var bound = (uint)universeSize;
            Random r = new Random(11);
            var hashFuncs = new Func<int, int>[_hashCount];
           for(int i = 0; i < _hashCount; i++)
            {
                uint a = (uint)r.Next(universeSize);
                uint b = (uint)r.Next(universeSize);
                uint c = (uint)r.Next(universeSize);
               hashFuncs[i] = hash => QHash(hash, a, b, c, bound);

            }
            return entity =>
            {
                var entityHash = _configuration.GetEntityHash(entity);
                return hashFuncs.Select(f => f(entityHash));
            };
        }


    /// <summary>
    /// Compute the hash for the given element.
    /// </summary>
    /// <param name="element"></param>
    private void ComputeMinHash(T element)
        {
            var idhash = _idHash(_configuration.GetId(element));
            var entityHashes = _hashFunctions(element).ToArray();
            for(int i = 0; i < _hashCount; i++)
            {
                if (entityHashes[i]< _slots[i, idhash])
                {
                    _slots[i, idhash] = entityHashes[i];
                }
            }
        }

        /// <summary>
        /// Create an array of the correct size.
        /// </summary>
        /// <param name="numHashFunctions"></param>
        /// <param name="setSize"></param>
        /// <returns></returns>
        private static int[,] GetMinHashSlots(int numHashFunctions, ulong setSize)
        {
            var minHashValues = new int[numHashFunctions, setSize];
            for (uint i = 0; i < numHashFunctions; i++)
            {
                for (ulong j = 0; j < setSize; j++)
                {
                    minHashValues[i, j] = Int32.MaxValue;
                }
            }
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
            int numHashFunctions, byte bitSize)
        {
            uint identicalMinHashes = 0;
            var unions = (long)numHashFunctions;
            if (minHashValues1 != null && minHashValues2 != null)
            {
                var bitRange = Enumerable.Range(0, bitSize).ToArray();
                var minHash1Length = minHashValues1.Count / bitSize;
                var minHash2Length = minHashValues2.Count / bitSize;
                var count = Math.Min(minHash1Length, minHash2Length);
                unions =  Math.Max(minHash1Length, minHash2Length);
                var idx = 0;
                for (int i = 0; i < count; i++)
                {
                    if (bitRange
                        .All(b => minHashValues1.Get(idx + b) == minHashValues2.Get(idx + b)))
                    {
                        identicalMinHashes++;
                    }
                    idx += bitSize;
                }
            }
            return (1.0D * identicalMinHashes) / unions;
        }
        #endregion
    }
}
