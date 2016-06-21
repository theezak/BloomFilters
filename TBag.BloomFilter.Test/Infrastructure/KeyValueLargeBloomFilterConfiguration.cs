namespace TBag.BloomFilter.Test.Infrastructure
{
    using System;
    using System.Text;
    using HashAlgorithms;
    using BloomFilters.Configurations;
    using BloomFilters.Invertible.Configurations;
    using BloomFilters.Countable.Configurations;/// <summary>
                                                /// A test Bloom filter configuration.
                                                /// </summary>
    internal class KeyValueLargeBloomFilterConfiguration : ReverseConfigurationBase<TestEntity, short>
    {
        private readonly IMurmurHash _murmurHash = new Murmur3();

        public KeyValueLargeBloomFilterConfiguration() : base(new ShortCountConfiguration())
        {}

        protected override long GetIdImpl(TestEntity entity)
        {
            return entity?.Id ?? 0L;
        }

        protected override int GetEntityHashImpl(TestEntity entity)
        {
            var res = BitConverter.ToInt32(_murmurHash.Hash(Encoding.UTF32.GetBytes(entity.Value)), 0);
            //extremely unlikely, but avoid zero as hash value at all cost
            return res == 0 ? 1 : res;
        }

        public override IFoldingStrategy FoldingStrategy { get; set; } = new SmoothNumbersFoldingStrategy();
    }
}