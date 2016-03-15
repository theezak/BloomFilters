namespace TBag.BloomFilter.Test
{
    using System;
    using System.Text;
    using TBag.BloomFilters;
    using TBag.HashAlgorithms;

    /// <summary>
    /// A test Bloom filter configuration.
    /// </summary>
    internal class DefaultBloomFilterConfiguration : KeyValueIbfConfigurationBase<TestEntity>
    {
        private readonly IMurmurHash _murmurHash = new Murmur3();

        protected override long GetIdImpl(TestEntity entity)
        {
            return entity?.Id ?? 0L;
        }
      
        protected override int GetEntityHashImpl(TestEntity entity)
        {
            var value = $"{entity.Value}";
            return BitConverter.ToInt32(_murmurHash.Hash(Encoding.Unicode.GetBytes(value), (uint)value.GetHashCode()), 0);
        }
    }
}