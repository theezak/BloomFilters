namespace System.Collections
{
    using System.Collections.Generic;

    /// <summary>
    /// Extensions for <see cref="BitArray"/>.
    /// </summary>
    public static class BitArrayExtensions
    {
        // <summary>
        // serialize a bitarray.
        // </summary>
        //<param name="bits"></param>
        // <returns></returns>
        public static IEnumerable<byte> ToBytes(this BitArray bits)
        {
            var prefix = BitConverter.GetBytes(bits.Count);
            foreach (var b in prefix)
                yield return b;
            int numBytes = bits.Count / 8;
            if (bits.Count % 8 != 0) numBytes++;
            byte[] bytes = new byte[numBytes];
            bits.CopyTo(bytes, 0);
            foreach (var b in bytes)
                yield return b;
        }
    }
}
