
namespace TBag.BloomFilters.Measurements.Test
{
    using Configurations;

    internal class KeyValuePairBloomFilterConfiguration : PairIbfConfigurationBase<sbyte>
    {

        public KeyValuePairBloomFilterConfiguration(
            ICountConfiguration<sbyte> configuration,
            bool createValueFilter = true) :
                base(configuration, createValueFilter)
        {
        }
    }
}
