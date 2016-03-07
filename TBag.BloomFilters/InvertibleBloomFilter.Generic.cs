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
    public class InvertibleBloomFilter<T, TId> : IInvertibleBloomFilter<T, TId>
    {
        #region Fields
        private readonly IBloomFilterConfiguration<T, int, TId, long> _configuration;
        private IInvertibleBloomFilterData<TId> _data = new InvertibleBloomFilterData<TId>();
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new Bloom filter, specifying an error rate of 1/capacity, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// A secondary hash function will be provided for you if your type T is either string or int. Otherwise an exception will be thrown. If you are not using these types please use the overload that supports custom hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        public InvertibleBloomFilter(ulong capacity) : this(capacity, null) { }

        /// <summary>
        /// Creates a new Bloom filter, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// A secondary hash function will be provided for you if your type T is either string or int. Otherwise an exception will be thrown. If you are not using these types please use the overload that supports custom hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="errorRate">The accepable false-positive rate (e.g., 0.01F = 1%)</param>
        public InvertibleBloomFilter(ulong capacity, int errorRate) : this(capacity, errorRate, null) { }

        /// <summary>
        /// Creates a new Bloom filter, specifying an error rate of 1/capacity, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="hashFunction">The function to hash the input values. Do not use GetHashCode(). If it is null, and T is string or int a hash function will be provided for you.</param>
        public InvertibleBloomFilter(ulong capacity, IBloomFilterConfiguration<T,int,TId,long> bloomFilterConfiguration) : 
            this(capacity, bestErrorRate(capacity), bloomFilterConfiguration) { }

        /// <summary>
        /// Creates a new Bloom filter, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="errorRate">The accepable false-positive rate (e.g., 0.01F = 1%)</param>
        /// <param name="hashFunction">The function to hash the input values. Do not use GetHashCode(). If it is null, and T is string or int a hash function will be provided for you.</param>
        public InvertibleBloomFilter(ulong capacity, float errorRate, IBloomFilterConfiguration<T,int,TId,long> bloomFilterConfiguration) : 
            this(bloomFilterConfiguration, bestM(capacity, errorRate), bestK(capacity, errorRate)) { }

        /// <summary>
        /// Creates a new Bloom filter.
        /// </summary>
        /// <param name="bloomFilterConfiguration">The function to hash the input values. Do not use GetHashCode(). If it is null, and T is string or int a hash function will be provided for you.</param>
        /// <param name="m">The number of elements in the BitArray.</param>
        /// <param name="k">The number of hash functions to use.</param>
        public InvertibleBloomFilter(
            IBloomFilterConfiguration<T, int, TId, long> bloomFilterConfiguration,
            long m,
            uint k)
        {
            // validate the params are in range
            if (m < 1) // from overflow in bestM calculation
                throw new ArgumentOutOfRangeException("The provided capacity and errorRate values would result in an array of length > int.MaxValue. Please reduce either the capacity or the error rate.");
            _configuration = bloomFilterConfiguration;
            if (_configuration.SplitByHash)
            {
                m = m / (long)k;
            }
            _data.HashFunctionCount = k;
            _data.BlockSize = m;
            var size = _data.BlockSize * _data.HashFunctionCount;
            _data.Counts = new int[size];
            _data.IdSums = new TId[size];
            _data.HashSums = new int[size];
        }
        #endregion

        #region Implementation of Bloom Filter public contract

     /// <summary>
     /// Add an item to the Bloom filter.
     /// </summary>
     /// <param name="item"></param>
        public void Add(T item)
        {
            var id = _configuration.GetId(item);
            var hashValue = _configuration.GetEntityHash(item);
            var idx = 0L;
            var hasRows = _data.HasRows();
            foreach (var position in _configuration.IdHashes(id, _data.HashFunctionCount).Select(p =>
            {
                var res = (p % _data.BlockSize) + idx;
                if (hasRows)
                {
                    idx += _data.BlockSize;
                }
                return res;
            }))
            {
                _data.Counts[position]++;
                _data.IdSums[position] = _configuration.IdXor(_data.IdSums[position], id);
                _data.HashSums[position] = _configuration.EntityHashXor(_data.HashSums[position], hashValue);
            }
        }

        /// <summary>
        /// Extract the Bloom filter in a serializable format.
        /// </summary>
        /// <returns></returns>
        public IInvertibleBloomFilterData<TId> Extract()
        {
            return _data;
        }

        /// <summary>
        /// Set the data for this Bloom filter.
        /// </summary>
        /// <param name="data"></param>
        public void Rehydrate(IInvertibleBloomFilterData<TId> data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (!data.IsValid())
                throw new ArgumentException("Invertible Bloom filter data is invalid.");
            _data = data; 
        }

        /// <summary>
        /// Remove the given item from the Bloom filter.
        /// </summary>
        /// <param name="item"></param>
        public void Remove(T item)
        {
            var id = _configuration.GetId(item);
            var hashValue = _configuration.GetEntityHash(item);
            var idx = 0L;
            var hasRows = _data.HasRows();
            foreach (var position in _configuration.IdHashes(id, _data.HashFunctionCount)
                .Select(p =>
            {
                var res = (p%_data.BlockSize) + idx;
                if (hasRows)
                {
                    idx += _data.BlockSize;
                }
                return res;
            }))
            {
                Remove(id, hashValue, position);
            }
        }

        /// <summary>
        /// Checks for the existance of the item in the filter for a given probability.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item)
        {
            var hash = _configuration.GetEntityHash(item);
            var idx = 0L;
            var hasRows = _data.HasRows();
            foreach (var position in _configuration.IdHashes(_configuration.GetId(item), _data.HashFunctionCount)
                .Select(p => {
                    var res = (p % _data.BlockSize) + idx;
                    if (hasRows)
                    {
                        idx += _data.BlockSize;
                    }
                    return res;
                }))
            {
                if (IsPure(_data, position))
                {
                    if (_data.HashSums[position] != hash)
                    {
                        return false;
                    }
                }
                else if (_data.Counts[position] == 0)
                {
                    return false;
                }
            }
            return true;
        }

        public void Subtract(InvertibleBloomFilter<T, TId> filter, HashSet<TId> idsWithChanges = null)
        {
            Subtract(filter.Extract(), idsWithChanges);
        }

        /// <summary>
        /// Fully subtract another Bloom filter. 
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="idsWithChanges">Optional list for identifiers of entities that we detected a change in value for.</param>
        /// <remarks>Original algorithm was focused on differences in the sets of Ids, not on differences in the value. This optionally also provides you list of Ids for entities with changes.</remarks>
        public void Subtract(IInvertibleBloomFilterData<TId> filter, HashSet<TId> idsWithChanges = null)
        {
            if (!filter.IsCompatibleWith(_data))
                throw new ArgumentException("Subtracted invertible Bloom filters are not compatible.");
            var detectChanges = idsWithChanges != null;
            for (long i = 0L; i < _data.Counts.LongLength; i++)
            {
                _data.Counts[i] -= filter.Counts[i];
                _data.HashSums[i] = _configuration.EntityHashXor(_data.HashSums[i], filter.HashSums[i]);
                var idXorResult = _configuration.IdXor(_data.IdSums[i], filter.IdSums[i]);
                if (detectChanges &&
                    !_configuration.IsEntityHashIdentity(_data.HashSums[i]) &&
                    _data.Counts[i] == 0 &&
                    IsPure(filter, i) &&
                    _configuration.IsIdIdentity(idXorResult))
                {
                    idsWithChanges.Add(_data.IdSums[i]);
                    //recognized the difference, not a decode error.
                    _data.IdSums[i] = _configuration.IdXor(_data.IdSums[i], _data.IdSums[i]);
                    continue;
                }
                _data.IdSums[i] = idXorResult;
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
            var pureList = Range(0L, _data.Counts.LongLength).Where(i => IsPure(_data, i)).Select(i => i).ToList();
            while (pureList.Any())
            {
                var pureIdx = pureList[0];
                pureList.RemoveAt(0);
                if (!IsPure(_data, pureIdx))
                {
                    if (_data.Counts[pureIdx] == 0 &&
                       !_configuration.IsEntityHashIdentity(_data.HashSums[pureIdx]) &&
                       _configuration.IsIdIdentity(_data.IdSums[pureIdx]) &&
                       idMap.ContainsKey($"{pureIdx}"))
                    {
                        //ID and counts nicely zeroed out, but the hash didn't. A changed value might have been hashed in.
                        //this does constitute a decode error, since we couldn't exactly identify the identity that caused the difference.
                        foreach (var associatedId in idMap[$"{pureIdx}"])
                        {
                            modifiedEntities.Add(associatedId);
                        }
                        idMap.Clear();
                    }
                    continue;
                }
                var count = _data.Counts[pureIdx];
                var id = _data.IdSums[pureIdx];
                if (count > 0)
                {
                    listA.Add(id);
                }
                else
                {
                    listB.Add(id);
                }
                var hash3 = _data.HashSums[pureIdx];
                var idx = 0L;
                var hasRows = _data.HasRows();
                foreach (var position in _configuration.IdHashes(id, _data.HashFunctionCount).Select(p =>
                {
                    var res = (p%_data.BlockSize) + idx;
                    if (hasRows)
                    {
                        idx += _data.BlockSize;
                    }
                    return res;
                }))
                {
                    Remove(id, hash3, position);
                    if (!idMap.ContainsKey($"{position}"))
                    {
                        idMap[$"{position}"] = new HashSet<TId>();
                    }

                    idMap[$"{position}"].Add(id);
                    if (IsPure(_data, position) && !pureList.Any(p => p == position))
                    {
                        pureList.Add(position);
                    }
                }
            }
            for (var position = 0L; position < _data.Counts.LongLength; position++)
            {
                if (!_configuration.IsIdIdentity(_data.IdSums[position]) ||
                    !_configuration.IsEntityHashIdentity(_data.HashSums[position]) ||
                        _data.Counts[position] != 0)
                    return false;
            }
            return true;
        }
        #endregion

        #region Methods
       

        private static bool IsPure(IInvertibleBloomFilterData<TId> data, long position)
        {
            return Math.Abs(data.Counts[position]) == 1;
        }

        /// <summary>
        /// Remove an item at the given position.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="position"></param>
        private void Remove(TId idValue, int hashValue, long position)
        {
            _data.Counts[position]--;
            _data.IdSums[position] = _configuration.IdXor(_data.IdSums[position], idValue);
            _data.HashSums[position] = _configuration.EntityHashXor(_data.HashSums[position], hashValue);
        }

        private static uint bestK(ulong capacity, float errorRate)
        {
            return (uint)Math.Abs(Math.Round(Math.Log(2.0) * bestM(capacity, errorRate) / capacity));
        }

        private static long bestM(ulong capacity, float errorRate)
        {
            return (long)Math.Ceiling(capacity * Math.Log(errorRate, (1.0 / Math.Pow(2, Math.Log(2.0)))));
        }

        private static float bestErrorRate(ulong capacity)
        {
            if (capacity == 0) return float.MaxValue;
            float c = (float)(1.0 / capacity);
            if (c != 0)
                return c;
            else
                return (float)Math.Pow(0.6185, int.MaxValue / capacity); // http://www.cs.princeton.edu/courses/archive/spring02/cs493/lec7.pdf
        }
        private static IEnumerable<long> Range(long start, long end)
        {
            for (long i = start; i < end; i++)
                yield return i;
        }
        #endregion
    }
}