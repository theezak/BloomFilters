using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;

namespace TBag.BloomFilter.Test
{
   using BloomFilters;
    using HashAlgorithms;

    /// <summary>
    /// A test Bloom filter configuration.
    /// </summary>
    internal class DefaultBloomFilterConfiguration : IbfConfigurationBase<TestEntity, sbyte>
    {

        public DefaultBloomFilterConfiguration() : base(new CountConfiguration())
        {
        }


        protected override long GetIdImpl(TestEntity entity)
        {
            return entity?.Id ?? 0L;
        }
    }
}