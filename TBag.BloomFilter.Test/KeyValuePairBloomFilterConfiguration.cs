using System;
using TBag.BloomFilters;

namespace TBag.BloomFilter.Test
{
    internal class KeyValuePairBloomFilterConfiguration : PairIbfConfigurationBase<sbyte>
    {       

        public KeyValuePairBloomFilterConfiguration(ICountConfiguration<sbyte> configuration, bool createValueFilter = true) : base(configuration, createValueFilter)
        {
        }
    }
}
