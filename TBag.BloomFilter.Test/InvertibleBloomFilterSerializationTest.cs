using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf.Meta;
using TBag.BloomFilters;

namespace TBag.BloomFilter.Test
{
    [TestClass]
    public class InvertibleBloomFilterSerializationTest
    {
        [TestMethod]
        public void IbfSerializationTest()
        {
            var addSize = 100000;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
             var size = (long)testData.Length;
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new InvertibleBloomFilter<TestEntity, long, sbyte>(size, configuration);
            foreach (var itm in testData)
            {
                bloomFilter.Add(itm);
            }
            var data = bloomFilter.Extract();
            var deserializedData = default(IInvertibleBloomFilterData<long, int, sbyte>);
            var typeModel = TypeModel.Create();
            typeModel.UseImplicitZeroDefaults = true;
             using (var memStream = new MemoryStream())
            {
                typeModel.Serialize(memStream, data);
                var str = Encoding.UTF8.GetString(memStream.ToArray());
            }
            using (var memStream = new MemoryStream())
            { 
            using (var stream = new GZipStream(memStream, CompressionLevel.Optimal, true))
            {
                typeModel.Serialize(stream, data);
                    stream.Flush();
            }            
            memStream.Position = 0;
                
                using (var stream = new GZipStream(memStream, CompressionMode.Decompress))
                {
                    deserializedData =
                        (IInvertibleBloomFilterData<long, int, sbyte>)
                            typeModel.Deserialize(stream, null, typeof (InvertibleBloomFilterData<long, int, sbyte>));
                }
            }            

        }
    }
}
