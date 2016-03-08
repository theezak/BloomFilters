namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
  
    /// <summary>
    /// An invertible Bloom filter supports removal and additions.
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity</typeparam>
    /// <typeparam name="TId">Type of the entity identifier</typeparam>
    /// <typeparam name="TCount">Type of the counter</typeparam>
       public class InvertibleBloomFilter<TEntity, TId,TCount> : IInvertibleBloomFilter<TEntity, TId,TCount>
        where TCount : struct
    {
        #region Fields
        private readonly IBloomFilterConfiguration<TEntity, int, TId, long, TCount> _configuration;
        private IInvertibleBloomFilterData<TId,TCount> _data = new InvertibleBloomFilterData<TId,TCount>();
        private readonly IEqualityComparer<TCount> _countEqualityComparer = EqualityComparer<TCount>.Default;
        private readonly IComparer<TCount> _countComparer = Comparer<TCount>.Default;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new Bloom filter, specifying an error rate of 1/capacity, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// A secondary hash function will be provided for you if your type T is either string or int. Otherwise an exception will be thrown. If you are not using these types please use the overload that supports custom hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        public InvertibleBloomFilter(long capacity) : this(capacity, null) { }

        /// <summary>
        /// Creates a new Bloom filter, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// A secondary hash function will be provided for you if your type T is either string or int. Otherwise an exception will be thrown. If you are not using these types please use the overload that supports custom hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="errorRate">The accepable false-positive rate (e.g., 0.01F = 1%)</param>
        public InvertibleBloomFilter(long capacity, int errorRate) : this(capacity, errorRate, null) { }

        /// <summary>
        /// Creates a new Bloom filter, specifying an error rate of 1/capacity, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="hashFunction">The function to hash the input values. Do not use GetHashCode(). If it is null, and T is string or int a hash function will be provided for you.</param>
        public InvertibleBloomFilter(long capacity, IBloomFilterConfiguration<TEntity,int,TId,long, TCount> bloomFilterConfiguration) : 
            this(capacity, BestErrorRate(capacity), bloomFilterConfiguration) { }

        /// <summary>
        /// Creates a new Bloom filter, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="errorRate">The accepable false-positive rate (e.g., 0.01F = 1%)</param>
        /// <param name="hashFunction">The function to hash the input values. Do not use GetHashCode(). If it is null, and T is string or int a hash function will be provided for you.</param>
        public InvertibleBloomFilter(long capacity, float errorRate, IBloomFilterConfiguration<TEntity,int,TId,long, TCount> bloomFilterConfiguration) : 
            this(capacity, bloomFilterConfiguration, BestCompressedM(capacity, errorRate), BestK(capacity, errorRate)) { }

        /// <summary>
        /// Creates a new Bloom filter.
        /// </summary>
        /// <param name="bloomFilterConfiguration">The function to hash the input values. Do not use GetHashCode(). If it is null, and T is string or int a hash function will be provided for you.</param>
        /// <param name="m">The number of elements in the BitArray.</param>
        /// <param name="k">The number of hash functions to use.</param>
        public InvertibleBloomFilter(long capacity,
            IBloomFilterConfiguration<TEntity, int, TId, long, TCount> bloomFilterConfiguration,
            long m,
            uint k)
        {
            // validate the params are in range
            if (m < 1) // from overflow in bestM calculation
                throw new ArgumentOutOfRangeException("The provided capacity and errorRate values would result in an array of length > int.MaxValue. Please reduce either the capacity or the error rate.");
            _configuration = bloomFilterConfiguration;
            if (_configuration.SplitByHash)
            {
                m = (long)Math.Ceiling(1.0D * m /k);
            }
            _data.HashFunctionCount = k;
            _data.BlockSize = m;
            var size = _data.BlockSize * _data.HashFunctionCount;
            if (!bloomFilterConfiguration.Supports((ulong)capacity, (ulong)size))
            {
                throw new ArgumentOutOfRangeException($"The size {size} of the Bloom filter is not large enough to hold {capacity} items.");
            }
            _data.Counts = new TCount[size];
            _data.IdSums = new TId[size];
            _data.HashSums = new int[size];
        }
        #endregion

        #region Implementation of Bloom Filter public contract

     /// <summary>
     /// Add an item to the Bloom filter.
     /// </summary>
     /// <param name="item"></param>
        public void Add(TEntity item)
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
                _data.Counts[position] = _configuration.CountIncrease(_data.Counts[position]);
                _data.IdSums[position] = _configuration.IdXor(_data.IdSums[position], id);
                _data.HashSums[position] = _configuration.EntityHashXor(_data.HashSums[position], hashValue);
            }
        }

        /// <summary>
        /// Extract the Bloom filter in a serializable format.
        /// </summary>
        /// <returns></returns>
        public IInvertibleBloomFilterData<TId,TCount> Extract()
        {
            return _data;
        }

        /// <summary>
        /// Set the data for this Bloom filter.
        /// </summary>
        /// <param name="data"></param>
        public void Rehydrate(IInvertibleBloomFilterData<TId,TCount> data)
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
        public void Remove(TEntity item)
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
        public bool Contains(TEntity item)
        {
            var hash = _configuration.GetEntityHash(item);
            var id = _configuration.GetId(item);
            var idx = 0L;
            var hasRows = _data.HasRows();
            var countIdentity = _configuration.CountIdentity();
            foreach (var position in _configuration.IdHashes(id, _data.HashFunctionCount)
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
                    if (_data.HashSums[position] != hash || 
                        !_configuration.IsIdIdentity(_configuration.IdXor(_data.IdSums[position], id)))
                    {
                        return false;
                    }
                }
                else if (_countEqualityComparer.Equals(_data.Counts[position], countIdentity))
                {
                    return false;
                }
            }
            return true;
        }

        public void Subtract(InvertibleBloomFilter<TEntity, TId, TCount> filter, HashSet<TId> idsWithChanges = null)
        {
            Subtract(filter.Extract(), idsWithChanges);
        }

        /// <summary>
        /// Fully subtract another Bloom filter. 
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="idsWithChanges">Optional list for identifiers of entities that we detected a change in value for.</param>
        /// <remarks>Original algorithm was focused on differences in the sets of Ids, not on differences in the value. This optionally also provides you list of Ids for entities with changes.</remarks>
        public void Subtract(IInvertibleBloomFilterData<TId, TCount> filter, HashSet<TId> idsWithChanges = null)
        {
            if (!filter.IsCompatibleWith(_data))
                throw new ArgumentException("Subtracted invertible Bloom filters are not compatible.");
            var detectChanges = idsWithChanges != null;
            var countsIdentity = _configuration.CountIdentity();
            for (long i = 0L; i < _data.Counts.LongLength; i++)
            {
                _data.Counts[i] = _configuration.CountSubtract(_data.Counts[i], filter.Counts[i]);
                _data.HashSums[i] = _configuration.EntityHashXor(_data.HashSums[i], filter.HashSums[i]);
                var idXorResult = _configuration.IdXor(_data.IdSums[i], filter.IdSums[i]);
                if (detectChanges &&
                    !_configuration.IsEntityHashIdentity(_data.HashSums[i]) &&
                    _countEqualityComparer.Equals(_data.Counts[i], countsIdentity) &&
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
            var countsIdentity = _configuration.CountIdentity();
            while (pureList.Any())
            {
                var pureIdx = pureList[0];
                pureList.RemoveAt(0);
                if (!IsPure(_data, pureIdx))
                {
                    if (_countEqualityComparer.Equals(_data.Counts[pureIdx], countsIdentity) &&
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
                if (_countComparer.Compare(count, countsIdentity) > 0)
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
                    if (IsPure(_data, position) && pureList.All(p => p != position))
                    {
                        pureList.Add(position);
                    }
                }
            }
            for (var position = 0L; position < _data.Counts.LongLength; position++)
            {
                if (!_configuration.IsIdIdentity(_data.IdSums[position]) ||
                    !_configuration.IsEntityHashIdentity(_data.HashSums[position]) ||
                        !_countEqualityComparer.Equals(_data.Counts[position], countsIdentity))
                    return false;
            }
            return true;
        }
        #endregion

        #region Methods
       

        private  bool IsPure(IInvertibleBloomFilterData<TId,TCount> data, long position)
        {
            return _configuration.IsPureCount(data.Counts[position]);
        }

        /// <summary>
        /// Remove an item at the given position.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="position"></param>
        private void Remove(TId idValue, int hashValue, long position)
        {
            _data.Counts[position] = _configuration.CountDecrease(_data.Counts[position]);
            _data.IdSums[position] = _configuration.IdXor(_data.IdSums[position], idValue);
            _data.HashSums[position] = _configuration.EntityHashXor(_data.HashSums[position], hashValue);
        }

        private static uint BestK(long capacity, float errorRate)
        {
            var k = (uint)Math.Ceiling(Math.Abs(Math.Log(2.0D) * (1.0D * BestM(capacity, errorRate) / capacity)));
            //at least 2 hash functions.
           return Math.Max(2, k);
        }

        private static long BestCompressedM(long capacity, float errorRate)
        {
            return (long)(BestM(capacity, errorRate) * Math.Log(2.0D));
        }
        private static long BestM(long capacity, float errorRate)
        {
            return (long) Math.Abs((capacity*Math.Log(errorRate))/Math.Pow(2, Math.Log(2.0D)));
        }

        /// <summary>
        /// This determines an error rate assuming that at higher capacity a higher error rate is acceptable as a trade off for space. Provide your own error rate if this does not work for you.
        /// </summary>
        /// <param name="capacity"></param>
        /// <returns></returns>
        private static float BestErrorRate(long capacity)
        {
            var errRate = Math.Min(0.5F, (float)(0.00001F *  Math.Pow(2.0D, Math.Log(capacity))));
            return Math.Min(0.5F, (float)Math.Pow(0.6185D, BestM(capacity, errRate)/capacity));          
            // http://www.cs.princeton.edu/courses/archive/spring02/cs493/lec7.pdf
        }

        private static IEnumerable<long> Range(long start, long end)
        {
            for (long i = start; i < end; i++)
                yield return i;
        }
        #endregion
    }
}