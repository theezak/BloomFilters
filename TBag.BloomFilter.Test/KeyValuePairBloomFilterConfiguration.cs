using System;
using TBag.BloomFilters;
using TBag.BloomFilters.Configurations;
using TBag.BloomFilters.Invertible.Configurations;

namespace TBag.BloomFilter.Test
{
    internal class KeyValuePairBloomFilterConfiguration : PairConfigurationBase<sbyte>
    {       

        public KeyValuePairBloomFilterConfiguration(ICountConfiguration<sbyte> configuration, bool createValueFilter = true) : 
            base(configuration, createValueFilter)
        {
        }

        public override IFoldingStrategy FoldingStrategy { get; set; } = new SmoothNumbersFoldingStrategy();
    }
}
