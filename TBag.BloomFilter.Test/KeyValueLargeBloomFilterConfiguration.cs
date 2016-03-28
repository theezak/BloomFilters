namespace TBag.BloomFilter.Test
{
    using System;
    using System.Text;
    using BloomFilters;
    using HashAlgorithms;
    using BloomFilters.Configurations;
    /// <summary>
    /// A test Bloom filter configuration.
    /// </summary>
    internal class KeyValueLargeBloomFilterConfiguration : ReverseIbfConfigurationBase<TestEntity, int>
    {
        private readonly IMurmurHash _murmurHash = new Murmur3();

        public KeyValueLargeBloomFilterConfiguration() : base(new IntCountConfiguration())
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

        protected override int GetEntityHashImpl(TestEntity entity)
        {
            return BitConverter.ToInt32(_murmurHash.Hash(Encoding.UTF32.GetBytes(entity.Value)), 0);
        }
    }
}