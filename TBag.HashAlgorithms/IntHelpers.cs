namespace TBag.HashAlgorithms
{ 
    /// <summary>
    ///     Integer helpers.
    /// </summary>
internal static class IntHelpers
    {
        /// <summary>
        ///     Rotate left
        /// </summary>
        /// <param name="original"></param>
        /// <param name="bits"></param>
        /// <returns></returns>
        public static ulong RotateLeft(this ulong original, int bits)
        {
            return (original << bits) | (original >> (64 - bits));
        }

        /// <summary>
        ///     Rotate right
        /// </summary>
        /// <param name="original"></param>
        /// <param name="bits"></param>
        /// <returns></returns>
        public static ulong RotateRight(this ulong original, int bits)
        {
            return (original >> bits) | (original << (64 - bits));
        }

        /// <summary>
        ///     Get unsigned int 64.
        /// </summary>
        /// <param name="bb"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static unsafe ulong GetUInt64(this byte[] bb, int pos)
        {
            // we only read aligned longs, so a simple casting is enough       
            fixed (byte* pbyte = &bb[pos])
            {
                return *(ulong*) pbyte;
            }
        }
    }
}