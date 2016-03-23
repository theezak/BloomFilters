using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBag.BloomFilters
{
   public static class CompressionExtensions
    {
        public static byte[] Compress(this int[] array)
        {
            if (array == null) return null;
            var distinctItems = new HashSet<int>(array);
            var codes = distinctItems.Select((k, i) => new KeyValuePair<int, int>(k, i)).ToDictionary(kv => kv.Key, kv => kv.Value);
            var bitSize = (int)Math.Ceiling(Math.Log(codes.Count) / Math.Log(2));
            var bitArray = new BitArray((int)(bitSize * array.Length) + (codes.Count * sizeof(int) * 8) + 3*(sizeof(int) * 8));
            var bitArrayIdx = 0;
            foreach (var b in BitConverter.GetBytes(bitSize))
            {
                bitArrayIdx = AddByte(bitArray, b, bitArrayIdx);
            }
            foreach (var b in BitConverter.GetBytes(codes.Count))
            {
                bitArrayIdx = AddByte(bitArray, b, bitArrayIdx);
            }
            foreach (var itm in codes.Keys)
            {
                bitArrayIdx = AddInt(bitArray, itm, bitArrayIdx);
            }
            foreach (var b in BitConverter.GetBytes(array.Length))
            {
                bitArrayIdx = AddByte(bitArray, b, bitArrayIdx);
            }
            foreach (var itm in array)
            {
                var location = codes[itm];
                var bytes = BitConverter.GetBytes(location);
                bitArrayIdx = AddInt(bitArray, location, bitArrayIdx, bitSize);
            }
            return bitArray.ToBytes();
        }

        public static int[] Decompress(this byte[] array)
        {
            var intSize = sizeof(Int32);
            var arrayIdx = 0;
            var bitSize = BitConverter.ToInt32(array, arrayIdx);
            arrayIdx += intSize;
            var codesLength = BitConverter.ToInt32(array, arrayIdx);
            arrayIdx += intSize;
            var codes = new int[codesLength];
            for(var j=0; j < codes.Length; j++)
            {
                codes[j] = BitConverter.ToInt32(array, arrayIdx);
                arrayIdx += intSize;
            }
            var resultSize = BitConverter.ToInt32(array, arrayIdx);
            arrayIdx += intSize;
            var results = new int[resultSize];
            var bitArray = new BitArray(array);
            var bitArrayIdx = arrayIdx * 8;
            for(var j = 0; j < results.Length; j++)
            {
                var codeIdx = 0;
                for(var b=0; b < bitSize; b++)
                { 
                    codeIdx = codeIdx | (bitArray[bitArrayIdx++] ? (1 << b) : 0);
                }
                results[j] = codes[codeIdx];
            }
            return results;
        }

        private static int AddInt(BitArray bitArray, int i, int pos, int maxSize)
        {
            var count = 0;
            foreach (var b in BitConverter.GetBytes(i))
            {
                if (count == maxSize) break;
                for (var j = 0; j < 8; j++)
                {
                    bitArray.Set(pos++, (b & (1 << j)) != 0);
                    count++;
                    if (count == maxSize) break;
                }

            }
            return pos;
        }

        private static int AddInt(BitArray bitArray, int i, int pos)
        {
            foreach (var b in BitConverter.GetBytes(i))
            {
                for (var j = 0; j < 8; j++)
                {
                    bitArray.Set(pos++, (b & (1 << j)) != 0);
                }

            }
            return pos;
        }

        private static int AddByte(BitArray bitArray, byte b, int pos)
        {
            for (var i = 0; i < 8; i++)
            {
                bitArray.Set(pos++, (b & (1 << i)) != 0);
            }
            return pos;
        }
    }
}
