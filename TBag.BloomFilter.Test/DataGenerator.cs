
namespace TBag.BloomFilter.Test
{
    using System;
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

        internal static void Modify(this IList<TestEntity> entities, int changeCount)
        {
            if (entities == null || changeCount == 0) return;
            var idSeed = long.MaxValue;
            var random = new MersenneTwister();
            var rand = new Random(11);
            for(int i=0; i < changeCount; i++)
            {
                var operation = random.NextInt32() % 3;
                if (operation == 0)
                {
                    var index = rand.Next(0, entities.Count - 1);
                    entities.RemoveAt(index);
                }
                else if (operation == 1)
                {
                    entities.Add(new TestEntity { Id = idSeed, Value = random.NextInt32() });
                    idSeed--;
                }
                else
                {
                    var index = rand.Next(0, entities.Count - 1);
                    entities[index].Value = random.NextInt32();
                }
            }

        }
    }
}
