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
    /// <remarks>Only determines set differences. It does not handle key/value pairs.</remarks>
    public class InvertibleBloomFilter<TEntity, TId,TCount> : IInvertibleBloomFilter<TEntity, TId,TCount>
        where TCount : struct
        where TId : struct
    {
        #region Properties
        protected IEqualityComparer<TCount> CountEqualityComparer = EqualityComparer<TCount>.Default;

        /// <summary>
        /// The configuration for theBloom filter.
        /// </summary>
        protected IBloomFilterConfiguration<TEntity, TId, int, int, TCount> Configuration { get; }

        /// <summary>
        /// The Bloom filter data.
        /// </summary>
        protected InvertibleBloomFilterData<TId, int, TCount> Data { get; private set; } = new InvertibleBloomFilterData<TId, int, TCount>();
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new Bloom filter using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        public InvertibleBloomFilter(
            long capacity, 
            IBloomFilterConfiguration<TEntity,TId,int, int, TCount> bloomFilterConfiguration) : this(
                capacity, 
                BestErrorRate(capacity), 
                bloomFilterConfiguration) { }

        /// <summary>
        /// Creates a new Bloom filter, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="errorRate">The accepable false-positive rate (e.g., 0.01F = 1%)</param>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        public InvertibleBloomFilter(
            long capacity, 
            float errorRate, 
            IBloomFilterConfiguration<TEntity,TId,int, int, TCount> bloomFilterConfiguration) : this(
                capacity, 
                BestCompressedM(capacity, errorRate), 
                BestK(capacity, errorRate),
                  bloomFilterConfiguration)
        { }

        /// <summary>
        /// Creates a new Bloom filter.
        /// </summary>
        /// <param name="capacity">The capacity (typically the size ofthe set to add)</param>
        /// <param name="m">The size of the Bloom filter</param>
        /// <param name="k">The number of hash functions to use.</param>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration.</param>
        public InvertibleBloomFilter(
            long capacity,
            long m,
            uint k,
            IBloomFilterConfiguration<TEntity, TId, int, int, TCount> bloomFilterConfiguration)
        {
            // validate the params are in range
            if (m < 1) // from overflow in bestM calculation
                throw new ArgumentOutOfRangeException(
                    nameof(m),
                    "The provided capacity and errorRate values would result in an array of length > long.MaxValue. Please reduce either the capacity or the error rate.");
            Configuration = bloomFilterConfiguration;
            Data.HashFunctionCount = k;
            Data.BlockSize = m;
            var size = Data.BlockSize*Data.HashFunctionCount;
            if (!bloomFilterConfiguration.Supports((ulong) capacity, (ulong) size))
            {
                throw new ArgumentOutOfRangeException(
                    $"The size {size} of the Bloom filter is not large enough to hold {capacity} items.");
            }
            Data.Counts = new TCount[size];
            Data.IdSums = new TId[size];
        }

        #endregion

        #region Implementation of Bloom Filter public contract

     /// <summary>
     /// Add an item to the Bloom filter.
     /// </summary>
     /// <param name="item"></param>
        public virtual void Add(TEntity item)
        {
            var id = Configuration.GetId(item);
            var hashValue = Configuration.EntityHashes(item, 1).First();
         foreach (
             var position in Configuration.IdHashes(id, Data.HashFunctionCount).Select(p =>Math.Abs(p%Data.Counts.LongLength))
             )
         {
             Data.Counts[position] = Configuration.CountIncrease(Data.Counts[position]);
             Data.IdSums[position] = Configuration.IdXor(Data.IdSums[position], id);
         }
        }

        /// <summary>
        /// Extract the Bloom filter in a serializable format.
        /// </summary>
        /// <returns></returns>
        public InvertibleBloomFilterData<TId,int, TCount> Extract()
        {
            return Data;
        }

        /// <summary>
        /// Set the data for this Bloom filter.
        /// </summary>
        /// <param name="data"></param>
        public void Rehydrate(IInvertibleBloomFilterData<TId, int, TCount> data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (!data.IsValid())
                throw new ArgumentException(
                    "Invertible Bloom filter data is invalid.",
                    nameof(data));
            Data = data.ConvertToBloomFilterData();
        }

        /// <summary>
        /// Remove the given item from the Bloom filter.
        /// </summary>
        /// <param name="item"></param>
        public virtual void Remove(TEntity item)
        {
            var id = Configuration.GetId(item);
            var hashValue = Configuration.EntityHashes(item, 1).First();
            foreach (var position in Configuration.IdHashes(id, Data.HashFunctionCount).Select(p => Math.Abs(p % Data.Counts.LongLength)))
            {
                Data.Remove(Configuration, id, hashValue, position);
            }
        }

        /// <summary>
        /// Checks for the existance of the item in the filter for a given probability.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        /// <remarks>Contains is purely based upon the identifier. An idea is however to utilize the empty hash array for an id hash to double check deletions.</remarks>
        public bool Contains(TEntity item)
        {
            var id = Configuration.GetId(item);
              var countIdentity = Configuration.CountIdentity();
            foreach (var position in Configuration.IdHashes(id, Data.HashFunctionCount).Select(p =>Math.Abs(p % Data.Counts.LongLength)))
            {
                if (IsPure(Configuration, Data, position))
                {
                    if (!Configuration.IsIdIdentity(Configuration.IdXor(Data.IdSums[position], id)))
                    {
                        return false;
                    }
                }
                else if (CountEqualityComparer.Equals(Data.Counts[position], countIdentity))
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
        public virtual bool SubtractAndDecode(IInvertibleBloomFilter<TEntity, TId, TCount> filter,
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
        public virtual bool SubtractAndDecode(IInvertibleBloomFilterData<TId, int, TCount> filter,
            HashSet<TId> listA,
            HashSet<TId> listB,
            HashSet<TId> modifiedEntities)
        {
            return Data.SubtractAndDecode(filter, Configuration, listA, listB, modifiedEntities);
        }

        #endregion

        #region Methods
    
        protected static bool IsPure<TPId, TPEntityHash>(
            IBloomFilterConfiguration<TEntity,TPId,TPEntityHash,int,TCount> configuration,
            IInvertibleBloomFilterData<TPId, TPEntityHash, TCount> data, long position)
            where TPId : struct
            where TPEntityHash : struct
        {
            return configuration.IsPureCount(data.Counts[position]);
        }

        protected static uint BestK(long capacity, float errorRate)
        {
             //at least 3 hash functions.
            return Math.Max(
                3, 
                (uint)Math.Ceiling(Math.Abs(Math.Log(2.0D) * (1.0D * BestM(capacity, errorRate) / capacity))));
        }

        protected static long BestCompressedM(long capacity, float errorRate)
        {
            //compress the size of the Bloom filter, by ln2.
            return (long)(BestM(capacity, errorRate) * Math.Log(2.0D));
        }

        protected static long BestM(long capacity, float errorRate)
        {
            return (long)Math.Abs((capacity * Math.Log(errorRate)) / Math.Pow(2, Math.Log(2.0D)));
        }

        /// <summary>
        /// This determines an error rate assuming that at higher capacity a higher error rate is acceptable as a trade off for space. Provide your own error rate if this does not work for you.
        /// </summary>
        /// <param name="capacity"></param>
        /// <returns></returns>
        /// <remarks>Error rates above 50% are filtered out.</remarks>
        protected static float BestErrorRate(long capacity)
        {
            //heuristic for determing an error rate: as capacity becomes larger, the accepted error rate increases.
            var errRate = Math.Min(0.5F, (float)(0.000001F * Math.Pow(2.0D, Math.Log(capacity))));
            //determine the best size based upon capacity and the error rate determined above, then calculate the error rate.
            return Math.Min(0.5F, (float)Math.Pow(0.5D, 1.0D * BestM(capacity, errRate) / capacity));
            // return Math.Min(0.5F, (float)Math.Pow(0.6185D, BestM(capacity, errRate) / capacity));
            // http://www.cs.princeton.edu/courses/archive/spring02/cs493/lec7.pdf
        }
        #endregion
    }
}