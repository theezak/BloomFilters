namespace TBag.BloomFilters.Measurements.Test
{
    using System;
    using System.Text;
    using BloomFilters;
    using HashAlgorithms;

    /// <summary>
    /// A test Bloom filter configuration.
    /// </summary>
    internal class DefaultBloomFilterConfiguration : StandardIbfConfigurationBase<TestEntity, sbyte>
    {
        public DefaultBloomFilterConfiguration() : base(new CountConfiguration())
        {
        }

        private readonly IMurmurHash _murmurHash = new Murmur3();

        protected override long GetIdImpl(TestEntity entity)
        {
            return entity?.Id ?? 0L;
        }
    }
}