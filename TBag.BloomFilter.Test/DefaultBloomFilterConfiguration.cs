using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;

namespace TBag.BloomFilter.Test
{
    using BloomFilters.Configurations;
    using BloomFilters.Invertible.Configurations;
    /// <summary>
    /// A test Bloom filter configuration.
    /// </summary>
    internal class DefaultBloomFilterConfiguration : ConfigurationBase<TestEntity, sbyte>
    {

        public DefaultBloomFilterConfiguration() : base(new ByteCountConfiguration())
        {
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

        protected override long GetIdImpl(TestEntity entity)
        {
            return entity?.Id ?? 0L;
        }

        public override IFoldingStrategy FoldingStrategy { get; set; }  = new SmoothNumbersFoldingStrategy();
    }
}