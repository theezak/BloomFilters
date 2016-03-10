namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// An invertible Bloom filter supports removal and additions.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="TCount"></typeparam>
    public class InvertibleBloomFilter<TEntity, TId,TCount> : IInvertibleBloomFilter<TEntity, TId,TCount>
        where TCount : struct
    {
        #region Fields
        private readonly IBloomFilterConfiguration<TEntity, int, TId, long, TCount> _configuration;
        private InvertibleBloomFilterData<TId,TCount> _data = new InvertibleBloomFilterData<TId,TCount>();
        private readonly IEqualityComparer<TCount> _countEqualityComparer = EqualityComparer<TCount>.Default;
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
        /// <param name="capacity"></param>
        /// <param name="bloomFilterConfiguration">The function to hash the input values. Do not use GetHashCode(). If it is null, and T is string or int a hash function will be provided for you.</param>
        /// <param name="m">The number of elements in the BitArray.</param>
        /// <param name="k">The number of hash functions to use.</param>
        public InvertibleBloomFilter(
            long capacity,
            IBloomFilterConfiguration<TEntity, int, TId, long, TCount> bloomFilterConfiguration,
            long m,
            uint k)
        {
            // validate the params are in range
            if (m < 1) // from overflow in bestM calculation
                throw new ArgumentOutOfRangeException(
                    nameof(m),
                    "The provided capacity and errorRate values would result in an array of length > long.MaxValue. Please reduce either the capacity or the error rate.");
            _configuration = bloomFilterConfiguration;          
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
             foreach (var position in _configuration.IdHashes(id, _data.HashFunctionCount).Select(p =>p % _data.Counts.LongLength))
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
        public InvertibleBloomFilterData<TId,TCount> Extract()
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
                throw new ArgumentException(
                    "Invertible Bloom filter data is invalid.",
                    nameof(data));
            _data = data.ConvertToBloomFilterData();
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
            foreach (var position in _configuration.IdHashes(id, _data.HashFunctionCount).Select(p => p % _data.Counts.LongLength))
            {
                _data.Remove(_configuration, id, hashValue, position);
            }
        }

        /// <summary>
        /// Checks for the existance of the item in the filter for a given probability.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        /// <summary>
        /// Checks for the existance of the item in the filter for a given probability.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(TEntity item)
        {
            var hash = _configuration.GetEntityHash(item);
            var id = _configuration.GetId(item);
              var countIdentity = _configuration.CountIdentity();
            foreach (var position in _configuration.IdHashes(id, _data.HashFunctionCount).Select(p => p % _data.Counts.LongLength))
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


        /// <summary>
        /// Subtract and then decode.
        /// </summary>
        /// <param name="filter">Bloom filter to subtract</param>
        /// <param name="listA">Items in this filter, but not in <paramref name="filter"/></param>
        /// <param name="listB">Items not in this filter, but in <paramref name="filter"/></param>
        /// <param name="modifiedEntities">Entities in both filters, but with a different value</param>
        /// <returns><c>true</c> when the decode was successful, otherwise <c>false</c></returns>
        public bool SubtractAndDecode(IInvertibleBloomFilter<TEntity, TId, TCount> filter,
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
            var k = (uint)Math.Ceiling(Math.Abs(Math.Log(2.0D) * (1.0D * BestM(capacity, errorRate) / capacity)));
            //at least 3 hash functions.
            return Math.Max(3, k);
        }

        private static long BestCompressedM(long capacity, float errorRate)
        {
            return (long)(BestM(capacity, errorRate) * Math.Log(2.0D));
        }
        private static long BestM(long capacity, float errorRate)
        {
            return (long)Math.Abs((capacity * Math.Log(errorRate)) / Math.Pow(2, Math.Log(2.0D)));
        }

        /// <summary>
        /// This determines an error rate assuming that at higher capacity a higher error rate is acceptable as a trade off for space. Provide your own error rate if this does not work for you.
        /// </summary>
        /// <param name="capacity"></param>
        /// <returns></returns>
        private static float BestErrorRate(long capacity)
        {
            var errRate = Math.Min(0.5F, (float)(0.000001F * Math.Pow(2.0D, Math.Log(capacity))));
            return Math.Min(0.5F, (float)Math.Pow(0.5D, 1.0D * BestM(capacity, errRate) / capacity));

            // return Math.Min(0.5F, (float)Math.Pow(0.6185D, BestM(capacity, errRate) / capacity));
            // http://www.cs.princeton.edu/courses/archive/spring02/cs493/lec7.pdf
        }
        #endregion
    }
}