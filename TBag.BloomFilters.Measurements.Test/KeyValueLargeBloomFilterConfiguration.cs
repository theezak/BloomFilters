namespace TBag.BloomFilters.Measurements.Test
{
    using System;
    using System.Text;
    using HashAlgorithms;
    using Configurations;
    using Invertible.Configurations;
    /// <summary>
    /// A test Bloom filter configuration.
    /// </summary>
    internal class KeyValueLargeBloomFilterConfiguration : ReverseConfigurationBase<TestEntity, int>
    {
        private readonly IMurmurHash _murmurHash = new Murmur3();

        public KeyValueLargeBloomFilterConfiguration() : base(new IntCountConfiguration())
        {}

        protected override long GetIdImpl(TestEntity entity)
        {
            return entity?.Id ?? 0L;
        }

        protected override int GetEntityHashImpl(TestEntity entity)
        {
            return BitConverter.ToInt32(_murmurHash.Hash(Encoding.UTF32.GetBytes(entity.Value)), 0);
        }
    }
}