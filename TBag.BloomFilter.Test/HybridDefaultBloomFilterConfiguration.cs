

namespace TBag.BloomFilter.Test
{
    using BloomFilters;
    using BloomFilters.Configurations;
    using HashAlgorithms;
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// A test Bloom filter configuration for hybrid Bloom filter.
    /// </summary>
    /// <remarks>Generates a full entity hash while keeping the standard pure implementation, knowing that the hybrid IBF won't use the entity hash except for internal the reverse IBF.</remarks>
    internal class HybridDefaultBloomFilterConfiguration : HybridIbfConfigurationBase<TestEntity, sbyte>
    {
        private readonly IMurmurHash _murmurHash = new Murmur3();
     
        /// <summary>
        /// Constructor
        /// </summary>
        public HybridDefaultBloomFilterConfiguration() : base(new ByteCountConfiguration())
        {
        }

        protected override long GetIdImpl(TestEntity entity)
        {
            return entity?.Id ?? 0L;
        }

        protected override int GetEntityHashImpl(TestEntity entity)
        {
            return BitConverter.ToInt32(_murmurHash.Hash(Encoding.UTF32.GetBytes(entity.Value)), 0);
        }

        public override IFoldingStrategy FoldingStrategy { get; set; } = new SmoothNumbersFoldingStrategy();
    }
}