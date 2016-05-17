namespace TBag.BloomFilters.Standard
{
    using System;
    using System.Collections;
    using Configurations;
    using System.Linq;
    using Invertible.Configurations;
    using System.Diagnostics.Contracts;    
    
    /// <summary>
    /// A Bloom filter
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The type of the key for <typeparamref name="TEntity"/></typeparam>
    public class BloomFilter<TEntity, TKey> : 
        IBloomFilter<TEntity, TKey> where TKey : struct
    {
        private readonly IEntityBloomFilterConfiguration<TEntity, TKey, int> _configuration;
        private FastBitArray _data;
        private uint _hashFunctionCount;
        private long _capacity;
        private long _blockSize;
        private float _errorRate;
        private static readonly byte[] EmptyByteArray = new byte[0];

        #region Properties

        /// <summary>
        /// When <c>true</c> the configuration will be validated, else <c>false</c>.
        /// </summary>
        protected bool ValidateConfiguration { get; set; }

        /// <summary>
        /// The item count.
        /// </summary>
        /// <remarks>Provide an estimate.</remarks>
        public virtual long ItemCount { get; private set; }

        /// <summary>
        /// The capacity.
        /// </summary>
        public virtual long Capacity => _capacity;

        /// <summary>
        /// The block size.
        /// </summary>
        public virtual long BlockSize => _blockSize;

        public IEntityBloomFilterConfiguration<TEntity, TKey, int> Configuration => _configuration;

        public uint HashFunctionCount => _hashFunctionCount;

        public float ErrorRate => _errorRate;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration"></param>
        public BloomFilter(IEntityBloomFilterConfiguration<TEntity, TKey, int> configuration)
        {
            _configuration = configuration;
        }

        #endregion

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="capacity">The capacity</param>
        /// <param name="foldFactor"></param>
        public void Initialize(long capacity, int foldFactor = 0)
        {
            Initialize(capacity, _configuration.BestErrorRate(capacity), foldFactor);
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="capacity">Capacity</param>
        /// <param name="errorRate">The desired error rate (between 0 and 1).</param>
        /// <param name="foldFactor"></param>
        public void Initialize(long capacity, float errorRate, int foldFactor = 0)
        {
            Initialize(
                capacity,
                _configuration.BestCompressedSize(capacity, errorRate, foldFactor),
                _configuration.BestHashFunctionCount(capacity, errorRate));
            _errorRate = errorRate;
        }

        /// <summary>
        /// Initialize the Bloom filter
        /// </summary>
        /// <param name="capacity">Capacity</param>
        /// <param name="m">Size per hash function</param>
        /// <param name="k">Number of hash functions.</param>
        public virtual void Initialize(long capacity, long m, uint k)
        {
            // validate the params are in range
            if (!_configuration.Supports(capacity, m))
            {
                throw new ArgumentOutOfRangeException(
                    $"The size {m} of the Bloom filter is not large enough to hold {capacity} items.");
            }
            _data = new FastBitArray((int) m);
            _hashFunctionCount = k;
            _capacity = capacity;
            _blockSize = m;
            _errorRate = _configuration.ActualErrorRate(_blockSize, _capacity, _hashFunctionCount);
        }

        /// <summary>
        /// Remove a value from the Bloom filter
        /// </summary>
        /// <param name="value"></param>
        /// <remarks>Not the best thing to do. Use a counting Bloom filter instead when you need removal. Throw a not supported exception instead?</remarks>
        public virtual void RemoveKey(TKey value)
        {
            RemoveKey(value, _configuration.IdHash(value));
        }

        /// <summary>
        /// Remove the given key
        /// </summary>
        /// <param name="key">The key to remove</param>
        /// <param name="hash">The entity hash</param>
        protected virtual void RemoveKey(TKey key, int hash)
        {
            if (ValidateConfiguration)
            {
                IsValidConfiguration(Configuration.IdHash(key), hash);
            }
            _data.Set((int)Configuration.Probe(BlockSize, _hashFunctionCount, hash).First(), false);
            ItemCount--;
        }


        /// <summary>
        /// Add the identifier and hash.
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="entityHash">The entity hash</param>
        protected virtual void Add(TKey key, int entityHash)
        {
            if (ValidateConfiguration)
            {
                IsValidConfiguration(Configuration.IdHash(key), entityHash);
            }
            foreach (int position in Configuration.Probe(BlockSize, _hashFunctionCount, entityHash))
            {
                _data.Set(position, true);
            }
            ItemCount++;
        }

        /// <summary>
        /// Determine if a value is in the Bloom filter.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual bool ContainsKey(TKey value)
        {
            return ContainsKey(value, _configuration.IdHash(value));
        }

        protected virtual bool ContainsKey(TKey key, int hash)
        {
            if (ValidateConfiguration)
            {
                IsValidConfiguration(Configuration.IdHash(key), hash);
            }
            return _configuration
                .Probe(BlockSize, _hashFunctionCount, hash)
                .All(position => _data.Get((int)position));
        }

        /// <summary>
        /// Extract the Bloom filter data
        /// </summary>
        /// <returns></returns>
        public virtual BloomFilterData Extract()
        {
            return new BloomFilterData
            {
                Bits = _data?.ToBytes(),
                Capacity = _capacity,
                BlockSize = _blockSize,
                ItemCount = ItemCount,
                HashFunctionCount = _hashFunctionCount
            };
        }

        /// <summary>
        /// Load the Bloom filter data into the Bloom filter
        /// </summary>
        /// <param name="bloomFilterData"></param>
        public virtual void Rehydrate(IBloomFilterData bloomFilterData)
        {
            if (bloomFilterData == null) return;
            _hashFunctionCount = bloomFilterData.HashFunctionCount;
            _capacity = bloomFilterData.Capacity;
            _blockSize = bloomFilterData.BlockSize;
            ItemCount = bloomFilterData.ItemCount;
            _data = new FastBitArray(bloomFilterData.Bits?? EmptyByteArray)
            {
                Length = (int)_blockSize
            };
        }

        /// <summary>
        /// Intersect with a Bloom filter. 
        /// </summary>
        /// <param name="bloomFilterData"></param>
        /// <remarks>Results in only retaining the keys the filters have in common.</remarks>
        public virtual void Intersect(IBloomFilterData bloomFilterData)
        {
            if (bloomFilterData == null) return;
            var foldedSize = _configuration.FoldingStrategy?.GetFoldFactors(_blockSize, bloomFilterData.BlockSize);
            if (bloomFilterData.BlockSize != BlockSize &&
                foldedSize == null)
            {
                throw new ArgumentException("Bloom filters of different sizes cannot be intersected.", nameof(bloomFilterData));
            }
            var otherBitArray = new FastBitArray(bloomFilterData.Bits ?? EmptyByteArray)
            {
                Length = (int)bloomFilterData.BlockSize
            };
            if (foldedSize != null &&
                (foldedSize.Item2 != 1 ||
                foldedSize.Item1 != 1))
            {
                if (foldedSize.Item1 != 1)
                {
                    Fold((uint)foldedSize.Item1, true);
                }
                if (foldedSize.Item2 != 1)
                {
                    for (var i = 0; i < _data.Length; i++)
                    {
                        _data.Set(i, _data.Get(i) & GetFolded(otherBitArray, i, (uint)foldedSize.Item2));
                    }
                    ItemCount = EstimateItemCount(_data, _hashFunctionCount);
                    return;
                }
            }
            _data = _data.And(otherBitArray);
            ItemCount = EstimateItemCount(_data, _hashFunctionCount);
        }

        /// <summary>
        /// Add a Bloom filter (union)
        /// </summary>
        /// <param name="bloomFilter"></param>
        /// <remarks>Results in all keys from the filters.</remarks>
        public virtual void Add(IBloomFilter<TEntity, TKey> bloomFilter)
        {
            if (bloomFilter == null) return;
            Add(bloomFilter.Extract());
        }

        /// <summary>
        /// Add a Bloom filter (union)
        /// </summary>
        /// <param name="bloomFilterData"></param>
        /// <remarks>Results in all keys from the filters.</remarks>
        public virtual void Add(IBloomFilterData bloomFilterData)
        {
            if (bloomFilterData == null) return;
            var foldedSize = _configuration.FoldingStrategy?.GetFoldFactors(_blockSize, bloomFilterData.BlockSize);
            if (bloomFilterData.BlockSize != BlockSize &
                foldedSize == null)
            {
                throw new ArgumentException("Bloom filters of different sizes cannot be added.", nameof(bloomFilterData));
            }
            var otherBitArray = new FastBitArray(bloomFilterData.Bits ?? EmptyByteArray)
            {
                Length = (int)bloomFilterData.BlockSize
            };
            if (foldedSize != null &&
                (foldedSize.Item2 != 1 ||
                foldedSize.Item1 != 1))
            {
                if (foldedSize.Item1 != 1)
                {
                    Fold((uint)foldedSize.Item1, true);
                }
                if (foldedSize.Item2 != 1)
                {
                    for (var i = 0; i < _data.Length; i++)
                    {
                        _data.Set(i, _data.Get(i) | GetFolded(otherBitArray, i, (uint)foldedSize.Item2));
                    }
                    ItemCount = ItemCount + bloomFilterData.ItemCount;
                    return;
                }
            }
            _data = _data.Or(otherBitArray);
            ItemCount = ItemCount + bloomFilterData.ItemCount;
        }

        /// <summary>
        /// Subtract the given Bloom filter, resulting in the difference.
        /// </summary>
        /// <param name="bloomFilterData"></param>
        /// <remarks>Results in the symmetric difference of the two Bloom filters.</remarks>
        public virtual void Subtract(IBloomFilterData bloomFilterData)
        {
            if (bloomFilterData == null) return;
            var foldedSize = _configuration.FoldingStrategy?.GetFoldFactors(_blockSize, bloomFilterData.BlockSize);
            if (bloomFilterData.BlockSize != BlockSize &
                foldedSize == null)
            {
                throw new ArgumentException("Bloom filters of different sizes cannot be subtracted.", nameof(bloomFilterData));
            }
            var otherBitArray = new FastBitArray(bloomFilterData.Bits ?? EmptyByteArray)
            {
                Length = (int)bloomFilterData.BlockSize
            };
            if (foldedSize != null &&
                (foldedSize.Item2 != 1 ||
                foldedSize.Item1 != 1))
            {
                if (foldedSize.Item1 != 1)
                {
                    Fold((uint)foldedSize.Item1, true);
                }
                if (foldedSize.Item2 != 1)
                {
                    for (var i = 0; i < _data.Length; i++)
                    {
                        _data.Set(i, _data.Get(i) ^ GetFolded(otherBitArray, i, (uint)foldedSize.Item2));
                    }
                    ItemCount = EstimateItemCount(_data, _hashFunctionCount);
                    return;
                }
            }
            _data = _data.Xor(otherBitArray);
            ItemCount = EstimateItemCount(_data, _hashFunctionCount);
        }

        /// <summary>
        /// Fold the Bloom filter by the given factor.
        /// </summary>
        /// <param name="factor"></param>
        /// <param name="inPlace"></param>
        /// <returns></returns>
        public virtual BloomFilter<TEntity, TKey> Fold(uint factor, bool inPlace = false)
        {
            var result = _data.Fold(factor, inPlace);
            if (inPlace)
            {
                _capacity = _capacity / factor;
                _blockSize = result.Length;
                return this;
            }
            var bloomFilter = new BloomFilter<TEntity, TKey>(_configuration);
            bloomFilter.Rehydrate(new BloomFilterData
            {
                Bits = result.ToBytes(),
                ItemCount = ItemCount,
                Capacity = _capacity/factor,
                BlockSize = result.Length,
                HashFunctionCount = _hashFunctionCount
            });
            return bloomFilter;
        }

        public BloomFilter<TEntity,TKey> Compress(bool inPlace = false)
        {
            var foldFactor = _configuration?
                .FoldingStrategy?
                .FindCompressionFactor(_configuration, _blockSize, _capacity, ItemCount);
            if (foldFactor == null) return null;
           
            return Fold(foldFactor.Value, inPlace);
        }

        private static bool GetFolded(FastBitArray bitArray, int position, uint foldFactor)
        {
            if (foldFactor == 1) return bitArray[position];
            var foldedSize = bitArray.Length/foldFactor;
            for (var i = 0; i < foldFactor; i++)
            {
                if (bitArray.Get(position + i * (int)foldedSize))
                    return true;
            }
            return false;
        }

        private static long EstimateItemCount(FastBitArray array, uint hashFunctionCount)
        {
            var bitCount = 0L;
            for (var i = 0; i < array.Length; i++)
            {
                if (array[i])
                {
                    bitCount++;
                }
            }
            return (long)Math.Abs(-array.Length / ((double)hashFunctionCount) * Math.Log(1 - bitCount / ((double)array.Length)));
        }

        public void Add(TEntity value)
        {
            Add(_configuration.GetId(value), _configuration.EntityHash(value));
;        }

        public void Remove(TEntity value)
        {
            RemoveKey(_configuration.GetId(value), _configuration.EntityHash(value));
        }

        public bool Contains(TEntity value)
        {
            return ContainsKey(_configuration.GetId(value), _configuration.EntityHash(value));
        }

        public void Intersect(IBloomFilter<TEntity, TKey> bloomFilterData)
        {
            Intersect(bloomFilterData?.Extract());
        }

        /// <summary>
        /// Determine if the configuration is valid.
        /// </summary>
        /// <param name="idHash"></param>
        /// <param name="entityHash"></param>
        /// <remarks>For regular IBFs the entity hash and identifier hash have to be equal.</remarks>
        protected virtual void IsValidConfiguration(int idHash, int entityHash)
        {
            if (idHash != entityHash)
                throw new InvalidOperationException("The configuration of the IBF does not satisfy that the IdHash and EntityHash are equal. For key-value pairs, please use a reverse IBF or hybrid IBF.");
            ValidateConfiguration = false;
        }

    }
}
