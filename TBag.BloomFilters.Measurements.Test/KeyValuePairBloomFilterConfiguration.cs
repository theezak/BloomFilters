using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBag.BloomFilters.Measurements.Test
{
    internal class KeyValuePairBloomFilterConfiguration : PairIbfConfigurationBase<sbyte>
    {       

        public KeyValuePairBloomFilterConfiguration(ICountConfiguration<sbyte> configuration, bool createValueFilter = true) : base(configuration, createValueFilter)
        {
        }
    }
}
