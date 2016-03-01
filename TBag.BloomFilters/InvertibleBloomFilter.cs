
namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// An invertible Bloom filter supports removal and additions.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>TODO: switch ID to long.</remarks>
    /// //TODO: type of Id can be anything, as long as you provide a XOR function.
    public class InvertibleBloomFilter<T, TId>
    {
        #region Fields
        private readonly IBloomFilterConfiguration<T, int, TId, int> _configuration;
        private readonly int SizeOfInt = sizeof(int);
        private uint hashFunctionCount;
        private readonly TId[,] IdSums;
        private readonly int[,] hashSums;
        private readonly int[,] Counts;
        private readonly Func<TId, uint, IEnumerable<int>> getIdHashes;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new Bloom filter, specifying an error rate of 1/capacity, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// A secondary hash function will be provided for you if your type T is either string or int. Otherwise an exception will be thrown. If you are not using these types please use the overload that supports custom hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        public InvertibleBloomFilter(int capacity) : this(capacity, null) { }

        /// <summary>
        /// Creates a new Bloom filter, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// A secondary hash function will be provided for you if your type T is either string or int. Otherwise an exception will be thrown. If you are not using these types please use the overload that supports custom hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="errorRate">The accepable false-positive rate (e.g., 0.01F = 1%)</param>
        public InvertibleBloomFilter(int capacity, int errorRate) : this(capacity, errorRate, null) { }

        /// <summary>
        /// Creates a new Bloom filter, specifying an error rate of 1/capacity, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="hashFunction">The function to hash the input values. Do not use GetHashCode(). If it is null, and T is string or int a hash function will be provided for you.</param>
        public InvertibleBloomFilter(int capacity, IBloomFilterConfiguration<T,int,TId,int> bloomFilterConfiguration) : 
            this(capacity, bestErrorRate(capacity), bloomFilterConfiguration) { }

        /// <summary>
        /// Creates a new Bloom filter, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="errorRate">The accepable false-positive rate (e.g., 0.01F = 1%)</param>
        /// <param name="hashFunction">The function to hash the input values. Do not use GetHashCode(). If it is null, and T is string or int a hash function will be provided for you.</param>
        public InvertibleBloomFilter(int capacity, float errorRate, IBloomFilterConfiguration<T,int,TId,int> bloomFilterConfiguration) : 
            this(capacity, errorRate, bloomFilterConfiguration, bestM(capacity, errorRate), bestK(capacity, errorRate)) { }

        /// <summary>
        /// Creates a new Bloom filter.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="errorRate">The accepable false-positive rate (e.g., 0.01F = 1%)</param>
        /// <param name="hashFunction">The function to hash the input values. Do not use GetHashCode(). If it is null, and T is string or int a hash function will be provided for you.</param>
        /// <param name="m">The number of elements in the BitArray.</param>
        /// <param name="k">The number of hash functions to use.</param>
        public InvertibleBloomFilter(
            int capacity, 
            float errorRate, 
            IBloomFilterConfiguration<T,int,TId,int> bloomFilterConfiguration, 
            int m, 
            uint k)
        {
            // validate the params are in range
            if (capacity < 1)
                throw new ArgumentOutOfRangeException("capacity", capacity, "capacity must be > 0");
            if (errorRate >= 1 || errorRate <= 0)
                throw new ArgumentOutOfRangeException("errorRate", errorRate, String.Format("errorRate must be between 0 and 1, exclusive. Was {0}", errorRate));
            if (m < 1) // from overflow in bestM calculation
                throw new ArgumentOutOfRangeException(String.Format("The provided capacity and errorRate values would result in an array of length > int.MaxValue. Please reduce either of these values. Capacity: {0}, Error rate: {1}", capacity, errorRate));
             hashFunctionCount = k;
            _configuration = bloomFilterConfiguration;
            uint rows = 1;
            if (_configuration.SplitByHash)
            {
                m = m / (int)k;
                rows = hashFunctionCount;
            }
            Counts = new int[rows,m];
            this.IdSums = new TId[rows,m];
            this.hashSums = new int[rows,m];
            getIdHashes = (id, hashCount) => _configuration.IdHashes(id, hashCount).Select(h => h % m);
           
        }
        #endregion

        #region Implementation of Bloom Filter public contract
        public void Add(T item)
        {
            var id = _configuration.GetId(item);
            //TODO: should actually be across T ? But then we can't decode well, because we can't know T at decoding time.
            var hashValue = _configuration.GetEntityHash(item);
            var row = 0;
            foreach(var position in getIdHashes(id, hashFunctionCount))
            {
                Add(id, hashValue, row, position);
                if (_configuration.SplitByHash) row++;
            }
        }

        /// <summary>
        /// Remove the given item from the Bloom filter.
        /// </summary>
        /// <param name="item"></param>
        public void Remove(T item)
        {
            var id = _configuration.GetId(item);
            var hashValue = _configuration.GetEntityHash(item);
            var row = 0;
            foreach (var position in getIdHashes(id, hashFunctionCount))
            {
                Remove(id, hashValue, row, position);
                if (_configuration.SplitByHash) row++;
            }
        }

        /// <summary>
        /// Add a new item at the given position in the filter.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="position"></param>
        private void Add(TId id, int valueHash, int row, int position)
        {
            Counts[row,position]++;
            IdSums[row,position] = _configuration.IdXor(IdSums[row,position], id);
            hashSums[row,position] = _configuration.EntityHashXor(hashSums[row,position], valueHash);
        }

        /// <summary>
        /// Checks for the existance of the item in the filter for a given probability.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item)
        {
            var hash = _configuration.GetEntityHash(item);
            var row = 0;
            foreach (var position in getIdHashes(_configuration.GetId(item), hashFunctionCount))
            {
                if (IsPure(row, position))
                {
                    if (hashSums[row, position] != hash)
                    {
                        return false;
                    }
                }
                else if (Counts[row, position] == 0)
                {
                    return false;
                }
                if (_configuration.SplitByHash) row++;
            }
            return true;
        }

        /// <summary>
        /// Fully subtract another Bloom filter. 
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="idsWithChanges">Optional list for identifiers of entities that we detected a change in value for.</param>
        /// <remarks>Original algorithm was focused on differences in the sets of Ids, not on differences in the value. This optionally also provides you list of Ids for entities with changes.</remarks>
        public void Subtract(InvertibleBloomFilter<T, TId> filter, HashSet<TId> idsWithChanges = null)
        {
            //TODO: throw nice exception when counts are different.
            if (filter == null || filter.Counts.Length != Counts.Length) return;
            var detectChanges = idsWithChanges != null;
            for (var row = 0; row < Counts.GetLength(0); row++)
            {
                for (int i = 0; i < Counts.GetLength(1); i++)
                {
                    Counts[row, i] -= filter.Counts[row, i];
                    hashSums[row, i] = _configuration.EntityHashXor(hashSums[row, i], filter.hashSums[row, i]);
                    var idXorResult = _configuration.IdXor(IdSums[row, i], filter.IdSums[row, i]);
                    if (detectChanges &&
                        !_configuration.IsHashIdentity(hashSums[row, i]) &&
                        Counts[row, i] == 0 &&
                        filter.IsPure(row, i) &&
                        _configuration.IsIdIdentity(idXorResult))
                    {
                        idsWithChanges.Add(IdSums[row, i]);
                    }
                    IdSums[row, i] = idXorResult;
                }
            }
        }

        /// <summary>
        /// Decode the Bloom filter.
        /// </summary>
        /// <param name="listA"></param>
        /// <param name="listB"></param>
        /// <returns></returns>
        public bool Decode(HashSet<TId> listA, HashSet<TId> listB, HashSet<TId> modifiedEntities)
        {
            var idMap = new Dictionary<string, HashSet<TId>>();
            var pureList = Enumerable.Range(0, Counts.GetLength(1)).
                SelectMany(i => Enumerable.Range(0, Counts.GetLength(0)).Where(r => IsPure(r, i)).Select(r => new { Row = r, Pos = i })).ToList();
            while (pureList.Any())
            {
                var idx = pureList[0];
                pureList.RemoveAt(0);
                if (!IsPure(idx.Row, idx.Pos))
                {
                    if (Counts[idx.Row, idx.Pos] == 0 &&
                       !_configuration.IsHashIdentity(hashSums[idx.Row, idx.Pos]) &&
                       _configuration.IsIdIdentity(IdSums[idx.Row, idx.Pos]) &&
                       idMap.ContainsKey($"{idx.Row},{idx.Pos}"))
                    {
                        //ID and counts nicely zeroed out, but the hash didn't. A changed value might have been hashed in.
                        foreach (var associatedId in idMap[$"{idx.Row},{idx.Pos}"])
                        {
                            modifiedEntities.Add(associatedId);
                        }
                        idMap.Clear();
                    }
                    continue;
                }
                var count = Counts[idx.Row, idx.Pos];
                var id = IdSums[idx.Row, idx.Pos];
                if (count > 0)
                {
                    listA.Add(id);
                }
                else
                {
                    listB.Add(id);
                }
                var hash3 = hashSums[idx.Row, idx.Pos];
                var row = 0;
                foreach (var position in getIdHashes(id, hashFunctionCount))
                {
                    Remove(id, hash3, row, position);
                    if (!idMap.ContainsKey($"{row},{position}"))
                    {
                        idMap[$"{row},{position}"] = new HashSet<TId>();
                    }

                    idMap[$"{row},{position}"].Add(id);
                    if (IsPure(row, position) && !pureList.Any(p => p.Row == row && p.Pos == position))
                    {
                        pureList.Add(new { Row = row, Pos = position });
                    }
                    if (_configuration.SplitByHash) row++;
                }
            }
            for (var row = 0; row < this.Counts.GetLength(0); row++)
            {
                for (var position = 0; position < this.Counts.GetLength(1); position++)
                {
                    if (!_configuration.IsIdIdentity(this.IdSums[row, position])) return false;
                    if (!_configuration.IsHashIdentity(this.hashSums[row, position])) return false;
                    if (Counts[row, position] != 0) return false;
                }
            }
            return true;
        }
        #endregion

        #region Methods
        private bool IsPure(int row, int position)
        {
            return Math.Abs(Counts[row, position]) == 1;
        }

        /// <summary>
        /// Remove an item at the given position.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="position"></param>
        private void Remove(TId idValue, int hashValue, int row, int position)
        {
            Counts[row,position]--;
            IdSums[row,position] = _configuration.IdXor(IdSums[row,position], idValue);
            hashSums[row,position] = _configuration.EntityHashXor(hashSums[row,position], hashValue);
        }

        private static uint bestK(int capacity, float errorRate)
        {
            return (uint)Math.Abs(Math.Round(Math.Log(2.0) * bestM(capacity, errorRate) / capacity));
        }

        private static int bestM(int capacity, float errorRate)
        {
            return (int)Math.Ceiling(capacity * Math.Log(errorRate, (1.0 / Math.Pow(2, Math.Log(2.0)))));
        }

        private static float bestErrorRate(int capacity)
        {
            float c = (float)(1.0 / capacity);
            if (c != 0)
                return c;
            else
                return (float)Math.Pow(0.6185, int.MaxValue / capacity); // http://www.cs.princeton.edu/courses/archive/spring02/cs493/lec7.pdf
        }
        #endregion


        ///// <summary>
        ///// Hashes a string using Bob Jenkin's "One At A Time" method from Dr. Dobbs (http://burtleburtle.net/bob/hash/doobs.html).
        ///// Runtime is suggested to be 9x+9, where x = input.Length. 
        ///// </summary>
        ///// <param name="input">The string to hash.</param>
        ///// <returns>The hashed result.</returns>
        //private static int hashString(T input)
        //{
        //    string s = input as string;
        //    int hash = 0;

        //    for (int i = 0; i < s.Length; i++)
        //    {
        //        hash += s[i];
        //        hash += (hash << 10);
        //        hash ^= (hash >> 6);
        //    }
        //    hash += (hash << 3);
        //    hash ^= (hash >> 11);
        //    hash += (hash << 15);
        //    return hash;
        //}
    }
}