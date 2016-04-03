
namespace TBag.BloomFilters.Measurements.Test
{
    using Configurations;
    using Invertible.Configurations;
    internal class KeyValuePairBloomFilterConfiguration : PairConfigurationBase<sbyte>
    {

        public KeyValuePairBloomFilterConfiguration(
            ICountConfiguration<sbyte> configuration,
            bool createValueFilter = true) :
                base(configuration, createValueFilter)
        {
        }
    }
}
