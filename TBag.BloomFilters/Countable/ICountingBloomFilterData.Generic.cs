using System;


namespace TBag.BloomFilters.Countable
{
    using Configurations;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using TBag.BloomFilters.Configurations;
    using TBag.BloomFilters.Standard;

    public interface ICountingBloomFilterData<TId, TCount> : IBloomFilterData
        where TId : struct
        where TCount : struct
    {
        /// <summary>
        /// The counts
        /// </summary>
        TCount[] Counts { get; set; }

        /// <summary>
        /// The IdSum provider.
        /// </summary>
        ICompressedArray<TCount> CountProvider { get; }

        /// <summary>
        /// Set the compression providers.
        /// </summary>
        /// <param name="configuration"></param>
        void SyncCompressionProviders<THash>(
            ICountingBloomFilterConfiguration<TId,THash, TCount> configuration)
            where THash : struct;
    }
}
