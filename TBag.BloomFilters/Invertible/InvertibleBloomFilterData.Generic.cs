
namespace TBag.BloomFilters.Invertible
{
    using System;
    using System.Linq;
    using System.Runtime.Serialization;
    using Configurations;
    using BloomFilters.Configurations;
    using System.Threading.Tasks;
    using System.Collections.Concurrent;
    using Countable.Configurations;
    /// <summary>
    /// Implementation of <see cref="IInvertibleBloomFilterData{TId, TEntityHash, TCount}"/>
    /// </summary>
    /// <typeparam name="TId">Type of the entity identifier</typeparam>
    /// <typeparam name="TCount">Type of the occurence count</typeparam>
    [DataContract, Serializable]
    public class InvertibleBloomFilterData<TId, THash,TCount> :
        IInvertibleBloomFilterData<TId, THash, TCount>
        where TCount : struct
         where TId : struct
        where THash : struct
    {
        #region Fields
        private ConcurrentDictionary<long, object> _locks = new ConcurrentDictionary<long, object>();
        private ICompressedArray<THash> _hashSumProvider;
        private ICompressedArray<TId> _idSumProvider;
         private THash[] _hashSums;
        private Func<long, bool> _membershipTest;
        private TId[] _idSums;
        private bool _hasDirtyProvider = true;
        #endregion

        #region Properties
        /// <summary>
        /// The number of items stored in the Bloom filter
        /// </summary>
        [DataMember(Order = 1)]
        public long ItemCount { get; set; }

        /// <summary>
        /// The number of cells for a single hash function.
        /// </summary>
        [DataMember(Order = 2)]
        public long BlockSize { get; set; }

        /// <summary>
        /// The number of hash functions
        /// </summary>
        [DataMember(Order = 3)]
        public uint HashFunctionCount { get; set; }

        /// <summary>
        /// An array of identifier (key) sums.
        /// </summary>
        [DataMember(Order = 4)]
        public TId[] IdSums
        {
            get {
                if (_idSumProvider != null && _hasDirtyProvider)
                    throw new InvalidOperationException($"{nameof(IdSums)} cannot be retrieved while the compressed array has not been synchronized. Please call SyncCompressionProviders first.");
                return _idSumProvider?.ToArray() ?? _idSums; }
            set
            {
                if (_idSumProvider != null)
                {
                    _idSumProvider.Load(value, BlockSize,  _membershipTest);                   
                    return;
                }
                _idSums = value;
                _hasDirtyProvider = true;
            }
        }

        public void ExecuteExclusively(long lockPosition, Action action)
        {
            lock (_locks.GetOrAdd(lockPosition, new object()))
            {
                try
                {
                    action?.Invoke();
                }
                finally
                {
                    object value;
                    _locks.TryRemove(lockPosition, out value);
                }
            }
        }

        /// <summary>
        /// An array of hash value sums.
        /// </summary>
        [DataMember(Order = 5)]
        public THash[] HashSums
        {
            get
            {
                if (_hashSumProvider != null && _hasDirtyProvider)
                    throw new InvalidOperationException($"{nameof(HashSums)} cannot be retrieved while the compressed array has not been synchronized. Please call SyncCompressionProviders first.");
                return _hashSumProvider?.ToArray() ?? _hashSums;
            }
            set
            {              
                if (_hashSumProvider != null)
                {
                    _hashSumProvider.Load(value, BlockSize,  _membershipTest);
                    return;
                }
                _hashSums = value;
                _hasDirtyProvider = true;
            }
        }

        /// <summary>
        /// The counts.
        /// </summary>
        [DataMember(Order = 6)]
        public TCount[] Counts { get; set; }

        /// <summary>
        /// The data for the reverse IBF
        /// </summary>
        /// <remarks>Only used by the hybrid IBF</remarks>
        [DataMember(Order = 7)]
        public InvertibleBloomFilterData<TId, THash, TCount> SubFilter { get; set; }


        /// <summary>
        /// <c>true</c> when the data is for a RIBF, else <c>false</c>.
        /// </summary>
        [DataMember(Order = 8)]
        public bool IsReverse { get; set; }

        /// <summary>
        /// The capacity
        /// </summary>
        [DataMember(Order = 9)]
        public long Capacity { get; set; }

        /// <summary>
        /// The error rate
        /// </summary>
        [DataMember(Order =10)]
        public float ErrorRate { get; set; }

        /// <summary>
        /// The hashSum provider.
        /// </summary>
        public ICompressedArray<THash> HashSumProvider => _hashSumProvider;

        /// <summary>
        /// The idSum provider.
        /// </summary>
        public ICompressedArray<TId> IdSumProvider => _idSumProvider;
        #endregion

        #region Compressed arrays

        /// <summary>
        /// Set the counter provider.
        /// </summary>
        /// <param name="configuration"></param>
        public void SyncCompressionProviders(
            ICountingBloomFilterConfiguration<TId, THash, TCount> configuration)
        {
            if (configuration == null)
                throw new ArgumentException("Configuration is null", nameof(configuration));
            if (_hasDirtyProvider)
            {
                _hasDirtyProvider = false;
                 _membershipTest = position => IsMember(configuration.CountConfiguration, this, position);
                _hashSumProvider = configuration.CompressedArrayFactory.Create<THash>();
                _hashSumProvider.Load(_hashSums, BlockSize, _membershipTest);
                _hashSums = null;
                _idSumProvider = configuration.CompressedArrayFactory.Create<TId>();
                _idSumProvider.Load(_idSums, BlockSize, _membershipTest);
                _idSums = null;
            }
        }

        /// <summary>
        /// The given <paramref name="position"/> is not considered relevant when the count for that position equals the identity.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="data"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private static bool IsMember(
            ICountConfiguration<TCount> configuration,
            InvertibleBloomFilterData<TId, THash, TCount> data, 
            long position)
        {
            return configuration
                .Comparer
                .Compare(
                    configuration.Identity,
                    data.Counts[position]) != 0;
        }

        /// <summary>
        /// Clear the data
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="configuration"></param>
        public void Clear<TEntity>(IInvertibleBloomFilterConfiguration<TEntity,TId,THash,TCount> configuration)
        {
            ItemCount = 0;
            SyncCompressionProviders(configuration);
            Parallel.ForEach(
               Partitioner.Create(0L, BlockSize),
               (range, state) =>
               {
                   for (var i = range.Item1; i < range.Item2; i++)
                   {
                       IdSumProvider[i] = configuration.IdIdentity;
                       HashSumProvider[i] = configuration.HashIdentity;
                       Counts[i] = configuration.CountConfiguration.Identity;
                   }
               });
        }
        #endregion
    }
}
