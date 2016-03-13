namespace TBag.BloomFilters
{
    using System.Collections.Generic;
    using System.Linq;
    using System;
    using HashAlgorithms;
    internal static class BloomFilterConfigurationExtensions
    {
        #region Types
        private class BloomFilterEntityHashConfigurationWrapper<TEntity, TId, TCount> : 
            BloomFilterConfigurationBase<TEntity,int,int,int,TCount>
             where TCount : struct
            where TId : struct
        {
             private readonly IBloomFilterConfiguration<TEntity, TId, int, int, TCount> _wrappedConfiguration;
            private Func<TEntity, int> _getId;
            private readonly IMurmurHash _murmurHash = new Murmur3();
            private readonly IXxHash _xxHash = new XxHash();

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="configuration"></param>
            public BloomFilterEntityHashConfigurationWrapper(
                IBloomFilterConfiguration<TEntity, TId, int, int, TCount> configuration) 
            {
                  _wrappedConfiguration = configuration;
                _getId = e => _wrappedConfiguration.EntityHashes(e, 1).First();
                IdXor = (id1, id2) => id1 ^ id2;
                IsIdIdentity = id1 => id1 == 0;
                IdHashes = (id, hashCount) =>
                {
                    //generate the given number of hashes.
                    var murmurHash = BitConverter.ToInt32( _murmurHash.Hash(BitConverter.GetBytes(id)), 0);
                    if (hashCount == 1) return new[] { murmurHash };
                    var hash2 = BitConverter
                .ToInt32(
                    _xxHash.Hash(
                        BitConverter.GetBytes(id),
                        unchecked((uint)(murmurHash % (uint.MaxValue - 1)))),
                    0);
                    return ComputeHash(
                        murmurHash,
                        hash2,
                        hashCount);
                };

            }

            public override Func<TCount, TCount> CountDecrease
            {
                get { return _wrappedConfiguration.CountDecrease; }
                set { _wrappedConfiguration.CountDecrease = value; }
            }

            public override Func<TEntity, int> GetId
            {
                get { return _getId; }
                set { _getId = value; }
            }          

            public override Func<TCount> CountIdentity
            {
                get { return _wrappedConfiguration.CountIdentity; }
                set { _wrappedConfiguration.CountIdentity = value; }
            }

            public override Func<TCount, TCount> CountIncrease
            {
                get { return _wrappedConfiguration.CountIncrease; }
                set { _wrappedConfiguration.CountIncrease = value; }
            }

            public override Func<TCount, TCount, TCount> CountSubtract
            {
                get { return _wrappedConfiguration.CountSubtract; }
                set { _wrappedConfiguration.CountSubtract = value; }
            }

            public override Func<TCount> CountUnity
            {
                get { return _wrappedConfiguration.CountUnity; }
                set { _wrappedConfiguration.CountUnity = value; }
            }

            public override Func<int,int,int> EntityHashXor {
                get { return _wrappedConfiguration.EntityHashXor; }
                set { _wrappedConfiguration.EntityHashXor = value; }
            }
          

            public override Func<TEntity, uint, IEnumerable<int>> EntityHashes
            {
                get { return _wrappedConfiguration.EntityHashes; }
                set { _wrappedConfiguration.EntityHashes = value; }
            }

            public override Func<TCount, bool> IsPureCount
            {
                get { return _wrappedConfiguration.IsPureCount; }
                set { _wrappedConfiguration.IsPureCount = value; }
            }

            public override Func<int, bool> IsEntityHashIdentity
            {
                get { return _wrappedConfiguration.IsEntityHashIdentity; }
                set { _wrappedConfiguration.IsEntityHashIdentity = value; }
            }

            public override bool Supports(ulong capacity, ulong size)
            {
                return _wrappedConfiguration.Supports(capacity, size);
            }

            /// <summary>
            /// Performs Dillinger and Manolios double hashing. 
            /// </summary>
            /// <param name="primaryHash"></param>
            /// <param name="secondaryHash"></param>
            /// <param name="hashFunctionCount"></param>
            /// <param name="seed"></param>
            /// <returns></returns>
            private static IEnumerable<int> ComputeHash(
                int primaryHash,
                int secondaryHash,
                uint hashFunctionCount,
                int seed = 0)
            {
                for (long j = seed; j < hashFunctionCount; j++)
                {
                    yield return unchecked((int)(primaryHash + j * secondaryHash));
                }
            }

        }

        private class BloomFilterEntityHashConfigurationValueWrapper<TEntity, TId, TCount> :
           BloomFilterConfigurationBase<TEntity, int, TId, int, TCount>
           where TCount : struct
            where TId: struct
        {
            private readonly IBloomFilterConfiguration<TEntity, TId, int, int, TCount> _wrappedConfiguration;
            private readonly IMurmurHash _murmurHash = new Murmur3();
            private readonly IXxHash _xxHash = new XxHash();

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="configuration"></param>
            public BloomFilterEntityHashConfigurationValueWrapper(
                IBloomFilterConfiguration<TEntity, TId, int, int, TCount> configuration) 
            {
                _wrappedConfiguration = configuration;
                IdXor = (id1, id2) => id1 ^ id2;
                IsIdIdentity = id1 => id1 == 0;
                IdHashes = (id, hashCount) =>
                {
                    //generate the given number of hashes.
                    var murmurHash = BitConverter.ToInt32(_murmurHash.Hash(BitConverter.GetBytes(id)), 0);
                    if (hashCount == 1) return new[] { murmurHash };
                    var hash2 = BitConverter
                .ToInt32(
                    _xxHash.Hash(
                        BitConverter.GetBytes(id),
                        unchecked((uint)(murmurHash % (uint.MaxValue - 1)))),
                    0);
                    return ComputeHash(
                        murmurHash,
                        hash2,
                        hashCount);
                };
            }

            private IEnumerable<TId> GetEntityHashes(TEntity entity, uint count)
            {
                var id = _wrappedConfiguration.GetId(entity);
                for (var j = 0; j < count; j++)
                {
                    yield return id;
                }
            }
            public override Func<TCount, TCount> CountDecrease
            {
                get { return _wrappedConfiguration.CountDecrease; }
                set { _wrappedConfiguration.CountDecrease = value; }
            }

            public override Func<TEntity, int> GetId
            {
                get { return e => _wrappedConfiguration.EntityHashes(e, 1).First(); }
                set
                {
                    throw new NotImplementedException("Setting the GetId method on a derived configuration for value hashing is not supported.");
                }
            }

            public override Func<TCount> CountIdentity
            {
                get { return _wrappedConfiguration.CountIdentity; }
                set { _wrappedConfiguration.CountIdentity = value; }
            }

            public override Func<TCount, TCount> CountIncrease
            {
                get { return _wrappedConfiguration.CountIncrease; }
                set { _wrappedConfiguration.CountIncrease = value; }
            }

            public override Func<TCount, TCount, TCount> CountSubtract
            {
                get { return _wrappedConfiguration.CountSubtract; }
                set { _wrappedConfiguration.CountSubtract = value; }
            }        

            public override Func<TCount> CountUnity
            {
                get { return _wrappedConfiguration.CountUnity; }
                set { _wrappedConfiguration.CountUnity = value; }
            }

            public override Func<TId, TId, TId> EntityHashXor
            {
                get { return _wrappedConfiguration.IdXor; }
                set { _wrappedConfiguration.IdXor = value; }
            }

            public override Func<TEntity, uint, IEnumerable<TId>> EntityHashes
            {
                get { return GetEntityHashes; }
                set
                {
                    throw new NotImplementedException("Setting the EntityHashes method on a derived configuration for value hashing is not supported.");
                }
            }

            public override Func<TCount, bool> IsPureCount
            {
                get { return _wrappedConfiguration.IsPureCount; }
                set { _wrappedConfiguration.IsPureCount = value; }
            }

            public override Func<TId, bool> IsEntityHashIdentity
            {
                get { return _wrappedConfiguration.IsIdIdentity; }
                set { _wrappedConfiguration.IsIdIdentity = value; }
            }

            public override bool Supports(ulong capacity, ulong size)
            {
                return _wrappedConfiguration.Supports(capacity, size);
            }
            /// <summary>
            /// Performs Dillinger and Manolios double hashing. 
            /// </summary>
            /// <param name="primaryHash"></param>
            /// <param name="secondaryHash"></param>
            /// <param name="hashFunctionCount"></param>
            /// <param name="seed"></param>
            /// <returns></returns>
            private static IEnumerable<int> ComputeHash(
                int primaryHash,
                int secondaryHash,
                uint hashFunctionCount,
                int seed = 0)
            {
                for (long j = seed; j < hashFunctionCount; j++)
                {
                    yield return unchecked((int)(primaryHash + j * secondaryHash));
                }
            }
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="TEntityHash"></typeparam>
        /// <typeparam name="THash"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <param name="configuration"></param>
        /// <returns></returns>
        /// <remarks>Remarkably strange plumbing: for estimators, we want to handle the entity hash as the identifier.</remarks>
        internal static IBloomFilterConfiguration<TEntity, int, int, int, TCount> ConvertToEntityHashId
            <TEntity, TId, TCount>(
            this IBloomFilterConfiguration<TEntity, TId, int, int, TCount> configuration)
            where TCount : struct
            where TId : struct
        {
            if (configuration == null) return null;
            return new BloomFilterEntityHashConfigurationWrapper<TEntity, TId, TCount>(configuration);
        }

        internal static IBloomFilterConfiguration<TEntity, int, TId, int, TCount> ConvertToValueHash
           <TEntity, TId,  TCount>(
           this IBloomFilterConfiguration<TEntity, TId, int, int, TCount> configuration)
           where TCount : struct
            where TId : struct
        {
            if (configuration == null) return null;
            return new BloomFilterEntityHashConfigurationValueWrapper<TEntity, TId, TCount>(configuration);
        }
    }
}
