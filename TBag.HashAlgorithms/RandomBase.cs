namespace TBag.HashAlgorithms
{
    using System;
 
    public abstract class RandomBase
    {

         public abstract uint NextUInt32();

         public virtual int NextInt32()
        {
            return unchecked((int)NextUInt32());
        }

         public virtual ulong NextUInt64()
        {
            return ((ulong)NextUInt32() << 32) | NextUInt32();
        }

         public virtual long NextInt64()
        {
            return ((long)NextUInt32() << 32) | NextUInt32();
        }

         public virtual void NextBytes(byte[] buffer)
        {
            var i = 0;
            uint r;
            while (i + 4 <= buffer.Length)
            {
                r = NextUInt32();
                buffer[i++] = (byte)r;
                buffer[i++] = (byte)(r >> 8);
                buffer[i++] = (byte)(r >> 16);
                buffer[i++] = (byte)(r >> 24);
            }
            if (i >= buffer.Length) return;
            r = NextUInt32();
            buffer[i++] = (byte)r;
            if (i >= buffer.Length) return;
            buffer[i++] = (byte)(r >> 8);
            if (i >= buffer.Length) return;
            buffer[i] = (byte)(r >> 16);
        }

           public virtual double NextDouble()
        {
             var r1 = NextUInt32();
            var r2 = NextUInt32();
            return (r1 * (double)(2 << 11) + r2) / (double)(2 << 53);
        }

    }

}
