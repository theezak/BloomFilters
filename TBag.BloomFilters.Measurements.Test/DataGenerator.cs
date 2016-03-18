
namespace TBag.BloomFilters.Measurements.Test
{
    using System;
    using System.Collections.Generic;
    using HashAlgorithms;

    /// <summary>
    /// Generate test data.
    /// </summary>
    internal static class DataGenerator
    {
        internal static IEnumerable<TestEntity> Generate()
        {
            var id = 1L;
            var mers = new MersenneTwister();
            while (id < long.MaxValue)
            {
                yield return new TestEntity { Id = id, Value = (long.MaxValue - id).ToString() };
                id++;
            }
        }

        /// <summary>
        /// Modifiy the given number of items in the list, either by adding, removing or modifying (done randomly).
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="changeCount"></param>
        internal static void Modify(this IList<TestEntity> entities, int changeCount)
        {
            if (entities == null || changeCount == 0) return;
            var added = new List<TestEntity>();
            var idSeed = long.MaxValue;
            var random = new MersenneTwister();
            var eIndex = 0;
            for(int i=0; i < changeCount; i++)
            {
                var operation = random.NextInt32() % 3;
                if (operation == 0 && eIndex < entities.Count)
                {
                     entities.RemoveAt(eIndex);
                }
                else if (operation == 1 && eIndex < entities.Count)
                {
                    entities[eIndex++].Value = random.NextInt32().ToString();
                }
                else 
                {
                    added.Add(new TestEntity { Id = idSeed, Value = random.NextInt32().ToString() });
                    idSeed--;
                }
               
            }
            foreach (var itm in added)
            {
                entities.Add(itm);
            }
        }
    }
}
