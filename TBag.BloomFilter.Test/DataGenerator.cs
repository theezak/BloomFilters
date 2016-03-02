
namespace TBag.BloomFilter.Test
{

    using System.Collections.Generic;
    using TBag.HashAlgorithms;

    internal static class DataGenerator
    {
        internal static IEnumerable<TestEntity> Generate()
        {
            var id = 1L;
            var mers = new MersenneTwister();
            while (id < long.MaxValue)
            {
                yield return new TestEntity { Id = id, Value = mers.NextInt32() };
                id++;
            }
        }
    }
}
