﻿using System.Collections.Generic;

namespace TBag.BloomFilters.Measurements.Test
{
    using Configurations;
  
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
            //just exploring some flexibility.
            _valueFilterConfiguration = new KeyValuePairBloomFilterConfiguration(new ByteCountConfiguration(), false);
        }

  
        protected override long GetIdImpl(TestEntity entity)
        {
            return entity?.Id ?? 0L;
        }

        public override IBloomFilterConfiguration<KeyValuePair<long, int>, long, int, sbyte> SubFilterConfiguration
        {
            get { return _valueFilterConfiguration; }
            set { _valueFilterConfiguration = value; }
        }
    }
}