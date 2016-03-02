namespace System
{

    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Extensions for <see cref="byte"/>.
    /// </summary>
    public static class ByteExtensions
    {
        /// <summary>
        /// restore a BitArray from the enumeration of bytes
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static BitArray ToBitArray(this IEnumerable<byte> bytes)
        {
            int numBits = BitConverter.ToInt32(bytes.Take(4).ToArray(), 0);
            var ba = new BitArray(bytes.Skip(4).ToArray());
            ba.Length = numBits;
            return ba;
        }
    }
}
