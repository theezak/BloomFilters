using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TBag.BloomFilters.Invertible;
using TBag.BloomFilter.Test.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace TBag.BloomFilter.Test.Invertible.Standard
{
    [TestClass]
    public class ParallelAddTest
    {
        [TestMethod]
        public void ParallelInvertibleAddTest()
        {
            var addSize = 10000;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
            var testData2 = DataGenerator.Generate().Skip(addSize).Take(addSize).ToArray();
            var errorRate = 0.001F;
            var size = testData.Length;
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new InvertibleBloomFilter<TestEntity, long, sbyte>(configuration);
            bloomFilter.Initialize(2*size, errorRate);
            Parallel.ForEach(Partitioner.Create(testData, true), d => bloomFilter.Add(d));

            var bloomFilter2 = new InvertibleBloomFilter<TestEntity, long, sbyte>(configuration);
            bloomFilter2.Initialize(2 * size, errorRate);
            Parallel.ForEach(Partitioner.Create(testData2, true), d => bloomFilter2.Add(d));
            bloomFilter.Add(bloomFilter2);
            var contained = testData.Union(testData2).Count(item => bloomFilter.Contains(item));
            Assert.AreEqual(contained, 2 * addSize, "Not all items found in added Bloom filters");
        }

        [TestMethod]
        public void ParallelInvertibleAddDifferentSizesTest()
        {
            var addSize = 10000;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
            var testData2 = DataGenerator.Generate().Skip(addSize).Take(addSize).ToArray();
            var errorRate = 0.001F;
            var size = testData.Length;
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new InvertibleBloomFilter<TestEntity, long, sbyte>(configuration);
            bloomFilter.Initialize(4 * size, errorRate);
            Parallel.ForEach(Partitioner.Create(testData, true), d => bloomFilter.Add(d));
           
            var bloomFilter2 = new InvertibleBloomFilter<TestEntity, long, sbyte>(configuration);
            //We have to create a foldable version.
            var data = bloomFilter.Extract();
            var foldFactor = configuration.FoldingStrategy.GetAllFoldFactors(data.BlockSize).Where(f=>f>1).OrderBy(f=> f).First();
            bloomFilter2.Initialize(addSize, data.BlockSize / foldFactor,  data.HashFunctionCount);
            Parallel.ForEach(Partitioner.Create(testData2, true), d => bloomFilter2.Add(d));

            //add the bloom filters.
            bloomFilter.Add(bloomFilter2);
            var contained = testData.Union(testData2).Count(item => bloomFilter.Contains(item));
            Assert.AreEqual(contained, 2 * addSize, "Not all items found in added Bloom filters");
        }
    }
}
