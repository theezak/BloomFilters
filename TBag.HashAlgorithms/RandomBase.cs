using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBag.HashAlgorithms
{
     public abstract class RandomBase
    {

         public abstract UInt32 NextUInt32();

         public virtual Int32 NextInt32()
        {
            return (Int32)NextUInt32();
        }

         public virtual UInt64 NextUInt64()
        {
            return ((UInt64)NextUInt32() << 32) | NextUInt32();
        }

         public virtual Int64 NextInt64()
        {
            return ((Int64)NextUInt32() << 32) | NextUInt32();
        }

         public virtual void NextBytes(byte[] buffer)
        {
            int i = 0;
            UInt32 r;
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
            buffer[i++] = (byte)(r >> 16);
        }

           public virtual double NextDouble()
        {
            UInt32 r1, r2;
            r1 = NextUInt32();
            r2 = NextUInt32();
            return (r1 * (double)(2 << 11) + r2) / (double)(2 << 53);
        }

    }

}
