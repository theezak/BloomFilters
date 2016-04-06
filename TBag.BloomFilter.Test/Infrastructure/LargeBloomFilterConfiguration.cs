namespace TBag.BloomFilter.Test.Infrastructure
{
    using BloomFilters.Configurations;
    using BloomFilters.Invertible.Configurations;
    /// <summary>
    /// A test Bloom filter configuration.
    /// </summary>
    internal class LargeBloomFilterConfiguration : KeyConfigurationBase<TestEntity, short>
    {
        public LargeBloomFilterConfiguration() : base(new ShortCountConfiguration())
        {}

        protected override long GetIdImpl(TestEntity entity)
        {
            return entity?.Id ?? 0L;
        }

        public override IFoldingStrategy FoldingStrategy { get; set; } = new SmoothNumbersFoldingStrategy();
    }
}