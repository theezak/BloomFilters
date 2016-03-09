﻿namespace System.Collections
{
    using Generic;

    /// <summary>
    /// Extensions for <see cref="BitArray"/>.
    /// </summary>
   internal static class BitArrayExtensions
    {
        // <summary>
        // serialize a bitarray.
        // </summary>
        //<param name="bits"></param>
        // <returns></returns>
        internal static byte[] ToBytes(this BitArray bits)
        {
            var numBytes = bits.Count / 8;
            if (bits.Count % 8 != 0) numBytes++;
           var bytes = new byte[numBytes];
            bits.CopyTo(bytes, 0);
            return bytes;
        }
    }
}
