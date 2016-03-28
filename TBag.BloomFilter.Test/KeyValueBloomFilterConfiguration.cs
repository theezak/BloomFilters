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
    internal class KeyValueBloomFilterConfiguration : ReverseIbfConfigurationBase<TestEntity, sbyte>
    {
        private readonly IMurmurHash _murmurHash = new Murmur3();

        public KeyValueBloomFilterConfiguration() : base(new ByteCountConfiguration())
        {
        }


        protected override long GetIdImpl(TestEntity entity)
        {
            return entity?.Id ?? 0L;
        }

        protected override int GetEntityHashImpl(TestEntity entity)
        {
            return BitConverter.ToInt32(_murmurHash.Hash(Encoding.UTF32.GetBytes(entity.Value )) , 0);
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
    }
}