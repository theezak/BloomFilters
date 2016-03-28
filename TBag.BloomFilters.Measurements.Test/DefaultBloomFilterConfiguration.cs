using System.Collections.Generic;

namespace TBag.BloomFilters.Measurements.Test
{
    using Configurations;
    using HashAlgorithms;

    /// <summary>
    /// A test Bloom filter configuration.
    /// </summary>
    internal class DefaultBloomFilterConfiguration : IbfConfigurationBase<TestEntity, sbyte>
    {
        private IBloomFilterConfiguration<KeyValuePair<long, int>, long, int, sbyte> _valueFilterConfiguration;
        public DefaultBloomFilterConfiguration() : base(new ByteCountConfiguration(), false)
        {
            //allows the reverse filter to only use PureCount or the pure function, while this configuration
            //considers both the hash value and the PureCount.
            _valueFilterConfiguration = new KeyValuePairBloomFilterConfiguration(new ByteCountConfiguration(), false);
        }

        private readonly IMurmurHash _murmurHash = new Murmur3();

        protected override long GetIdImpl(TestEntity entity)
        {
            return entity?.Id ?? 0L;
        }

        /// <summary>
        /// Determine if an IBF, given this configuration and the given <paramref name="capacity"/>, will support a set of the given size.
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public override bool Supports(long capacity, long size)
        {
            return (sbyte.MaxValue - 15) * size > capacity;
        }

        public override IBloomFilterConfiguration<KeyValuePair<long, int>, long, int, sbyte> SubFilterConfiguration
        {
            get { return _valueFilterConfiguration; }
            set { _valueFilterConfiguration = value; }
        }
    }
}