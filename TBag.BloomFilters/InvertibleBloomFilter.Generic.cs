namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// An invertible Bloom filter supports removal and additions.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="TCount"></typeparam>
    public class InvertibleBloomFilter<T, TId,TCount> : IInvertibleBloomFilter<T, TId,TCount>
        where TCount : struct
    {
        #region Fields
        private readonly IBloomFilterConfiguration<T, int, TId, long, TCount> _configuration;
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
        public InvertibleBloomFilter(long capacity, IBloomFilterConfiguration<T,int,TId,long, TCount> bloomFilterConfiguration) : 
            this(capacity, BestErrorRate(capacity), bloomFilterConfiguration) { }

        /// <summary>
        /// Creates a new Bloom filter, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="errorRate">The accepable false-positive rate (e.g., 0.01F = 1%)</param>
        /// <param name="hashFunction">The function to hash the input values. Do not use GetHashCode(). If it is null, and T is string or int a hash function will be provided for you.</param>
        public InvertibleBloomFilter(long capacity, float errorRate, IBloomFilterConfiguration<T,int,TId,long, TCount> bloomFilterConfiguration) : 
            this(bloomFilterConfiguration, BestM(capacity, errorRate), BestK(capacity, errorRate)) { }

        /// <summary>
        /// Creates a new Bloom filter.
        /// </summary>
        /// <param name="bloomFilterConfiguration">The function to hash the input values. Do not use GetHashCode(). If it is null, and T is string or int a hash function will be provided for you.</param>
        /// <param name="m">The number of elements in the BitArray.</param>
        /// <param name="k">The number of hash functions to use.</param>
        public InvertibleBloomFilter(
            IBloomFilterConfiguration<T, int, TId, long, TCount> bloomFilterConfiguration,
            long m,
            uint k)
        {
            // validate the params are in range
            if (m < 1) // from overflow in bestM calculation
                throw new ArgumentOutOfRangeException("The provided capacity and errorRate values would result in an array of length > int.MaxValue. Please reduce either the capacity or the error rate.");
            _configuration = bloomFilterConfiguration;
            if (_configuration.SplitByHash)
            {
                m = (long)Math.Ceiling(m / (1.0D)*k);
            }
            _data.HashFunctionCount = k;
            _data.BlockSize = m;
            var size = _data.BlockSize * _data.HashFunctionCount;
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
                _data.Remove(_configuration, id, hashValue, position);
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
            var countIdentity = _configuration.CountIdentity();
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
                else if (_countEqualityComparer.Equals(_data.Counts[position], countIdentity))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Subtract and then decode.
        /// </summary>
        /// <param name="filter">Bloom filter to subtract</param>
        /// <param name="listA">Items in this filter, but not in <paramref name="filter"/></param>
        /// <param name="listB">Items not in this filter, but in <paramref name="filter"/></param>
        /// <param name="modifiedEntities">Entities in both filters, but with a different value</param>
        /// <returns><c>true</c> when the decode was successful, otherwise <c>false</c></returns>
        public bool SubtractAndDecode(IInvertibleBloomFilter<T, TId, TCount> filter,
            HashSet<TId> listA, 
            HashSet<TId> listB, 
            HashSet<TId> modifiedEntities)
        {
            return SubtractAndDecode(filter.Extract(), listA, listB, modifiedEntities);
        }

        /// <summary>
        /// Subtract and then decode.
        /// </summary>
        /// <param name="filter">Bloom filter to subtract</param>
        /// <param name="listA">Items in this filter, but not in <paramref name="filter"/></param>
        /// <param name="listB">Items not in this filter, but in <paramref name="filter"/></param>
        /// <param name="modifiedEntities">Entities in both filters, but with a different value</param>
        /// <returns><c>true</c> when the decode was successful, otherwise <c>false</c></returns>
        public bool SubtractAndDecode(IInvertibleBloomFilterData<TId, TCount> filter,
            HashSet<TId> listA,
            HashSet<TId> listB,
            HashSet<TId> modifiedEntities)
        {
            return _data.SubtractAndDecode(filter, _configuration, listA, listB, modifiedEntities);
        }

        /// <summary>
        /// Decode the Bloom filter.
        /// </summary>
        /// <param name="listA"></param>
        /// <param name="listB"></param>
        /// <returns></returns>
        public bool Decode(HashSet<TId> listA, HashSet<TId> listB, HashSet<TId> modifiedEntities)
        {
            return Extract().Decode(_configuration, listA, listB, modifiedEntities);
        }
        #endregion

        #region Methods
    
        private  bool IsPure(IInvertibleBloomFilterData<TId,TCount> data, long position)
        {
            return _configuration.IsPureCount(data.Counts[position]);
        }      

        private static uint BestK(long capacity, float errorRate)
        {
            var k = (uint)(Math.Abs(Math.Log(2.0D)*BestM(capacity, errorRate)/capacity));
            //at least 2 hash functions.
           return Math.Max(2, k);
        }

        private static long BestM(long capacity, float errorRate)
        {
            return (long) Math.Abs(Math.Ceiling(capacity*Math.Log(errorRate)/Math.Pow(2, Math.Log(2.0D))));
        }

        private static float BestErrorRate(long capacity)
        {
            if (capacity == 0) return 0.0F;
            if (capacity <= 1000)
            {
                return 1.0F/capacity;
            }
            //based upon article below, 70% of size gives similar error rate
            return (float)Math.Pow(0.6185D, 0.7D*BestM(capacity, 0.01F)/capacity);          
            //return (float) Math.Pow(0.6185, int.MaxValue / capacity);
            // http://www.cs.princeton.edu/courses/archive/spring02/cs493/lec7.pdf
        }
        #endregion
    }
}