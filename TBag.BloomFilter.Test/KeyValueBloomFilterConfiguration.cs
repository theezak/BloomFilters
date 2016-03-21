namespace TBag.BloomFilter.Test
{
    using System;
    using System.Text;
    using BloomFilters;
    using HashAlgorithms;

    /// <summary>
    /// A test Bloom filter configuration.
    /// </summary>
    internal class KeyValueBloomFilterConfiguration : KeyValueIbfConfigurationBase<TestEntity, sbyte>
    {
        private readonly IMurmurHash _murmurHash = new Murmur3();

        public KeyValueBloomFilterConfiguration() : base(new CountConfiguration())
        {
        }


        protected override long GetIdImpl(TestEntity entity)
        {
            return entity?.Id ?? 0L;
        }

        protected override int GetEntityHashImpl(TestEntity entity)
        {
            return BitConverter.ToInt32(_murmurHash.Hash(Encoding.UTF32.GetBytes(entity.Value )) , 0);
        }
    }
}