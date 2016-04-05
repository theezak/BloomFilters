namespace TBag.BloomFilters.Measurements.Test
{
    using Configurations;
    using Invertible.Configurations;
    /// <summary>
    /// A test Bloom filter configuration.
    /// </summary>
    internal class LargeBloomFilterConfiguration : KeyConfigurationBase<TestEntity, int>
    {
        public LargeBloomFilterConfiguration() : base(new IntCountConfiguration())
        {}

        protected override long GetIdImpl(TestEntity entity)
        {
            return entity?.Id ?? 0L;
        }

        /// <summary>
        /// Determine if an IBF, given this configuration and the given <paramref name="capacity"/>, will support a set of the given size.
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public override bool Supports(long capacity, long size)
        {
            return (int.MaxValue - 30) * size > capacity;
        }
    }
}