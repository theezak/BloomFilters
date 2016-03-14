namespace TBag.BloomFilters
{
    using System.Collections.Generic;
   
    /// <summary>
    /// A default Bloom filter configuration for identifiers with a precomputed value hash. Well suited for Bloom filters that are utilized according to their capacity.
    /// </summary>
    public abstract class HighUtilizationValuePairBloomFilterConfiguration :
        HighUtilizationBloomFilterConfigurationBase<KeyValuePair<long,int>>
    {
      
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="createValueFilter"></param>
        protected HighUtilizationValuePairBloomFilterConfiguration(bool createValueFilter = true) : 
            base(createValueFilter)
        {
        }

        protected override long GetIdImpl(KeyValuePair<long, int> entity)
        {
            return entity.Key;
        }

        protected override int GetEntityHashImpl(KeyValuePair<long, int> entity)
        {
            return entity.Value;
        }
    }
}

