using System;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TBag.BloomFilter.Test.Collections
{
    /// <summary>
    /// Summary description for FastBitArrayTest
    /// </summary>
    [TestClass]
    public class FastBitArrayTest
    {      

        [TestMethod]
        public void FastBitArrayFromBytes()
        {
            var bytes = Enumerable.Range(0, 1000000).SelectMany(v => BitConverter.GetBytes(v)).ToArray();
            var timer = new Stopwatch();
            timer.Start();
            var fast = new FastBitArray(bytes);
            timer.Stop();
            Console.WriteLine($"FastBitArray Time: {timer.ElapsedMilliseconds} ms");
            timer.Reset();
            timer.Start();
            var bitArray = new BitArray(bytes);
            timer.Stop();
            Console.WriteLine($"BitArray Time: {timer.ElapsedMilliseconds} ms");
            Assert.AreEqual(bitArray.Length, fast.Length, "FastBitArray has different length than BitArray");
            Assert.IsTrue(Enumerable.Range(0, fast.Length).All(i=>fast[i]==bitArray[i]), "BitArray and FastBitArray have different values");
        }
    }
}
