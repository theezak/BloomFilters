namespace TBag.BloomFilter.Test
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using BloomFilters.Configurations;
    using BloomFilters.Invertible;
    using Infrastructure;
    /// <summary>
    /// Summary description for SmoothNumberTest
    /// </summary>
    [TestClass]
    public class SmoothNumberTest
    {      
        /// <summary>
        /// Test smooth number generator.
        /// </summary>
        [TestMethod]
        public void SmoothNumbers()
        {
            var smootNumbers = new SmoothNumberGenerator().GetSmoothNumbers(100L, 100L, 5000L);
        }

        /// <summary>
        /// Test folding
        /// </summary>
        [TestMethod]
        public void SmoothNumbersFolding()
        {
            //the configuration already has the smooth number folding strategy set.
            var config =new  DefaultBloomFilterConfiguration();                    
           var bloomFilter = new InvertibleBloomFilter<TestEntity, long, sbyte>(config);
            bloomFilter.Initialize(100000, 0.001F);
            foreach (var itm in DataGenerator.Generate().Take(500).ToArray())
            {
                bloomFilter.Add(itm);
            }
            var bloomFilterData = bloomFilter.Extract();
            //find a fold factor based upon the Bloom filter size, the capacity and the actual keys used.          
            var fold = config.FoldingStrategy.FindFoldFactor(bloomFilterData.BlockSize, bloomFilterData.Capacity, bloomFilterData.ItemCount);
            if (fold.HasValue)
            {
                var res = bloomFilter.Fold(fold.Value);
            }
            var compressed = bloomFilter.Compress();
        }

        [TestMethod]
        public void SmoothNumbersSubtract()
        {
            //the configuration already has the smooth number folding strategy set.
            var config = new KeyValueBloomFilterConfiguration();
            var bloomFilter = new InvertibleReverseBloomFilter<TestEntity, long, sbyte>(config);
            bloomFilter.Initialize(100000, 0.001F);
            foreach (var itm in DataGenerator.Generate().Take(500).ToArray())
            {
                bloomFilter.Add(itm);
            }
            var bloomFilterData = bloomFilter.Extract();
            //find a fold factor based upon the Bloom filter size, the capacity and the actual keys used.          
            var fold = config.FoldingStrategy.FindFoldFactor(bloomFilterData.BlockSize, bloomFilterData.Capacity, bloomFilterData.ItemCount);
            var folded = bloomFilter.Compress();
            var hashSet = new HashSet<long>();
            foreach (var itm in DataGenerator.Generate().Skip(500).Take(100).ToArray())
            {
                bloomFilter.Add(itm);
            }
            var res = folded.SubtractAndDecode(bloomFilter, hashSet, hashSet, hashSet);
        }
    }
}
