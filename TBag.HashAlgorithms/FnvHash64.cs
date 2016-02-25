using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBag.HashAlgorithms
{
    public class FnvHash64 : IFnvHash64
    {
        private const ulong FnvPrime = unchecked(1099511628211);
        private const ulong ModuloValue = ulong.MaxValue;
        private const ulong FnvOffsetBasis = unchecked(14695981039346656037);

        public byte[] Hash(byte[] array, uint seed = 0)
        {
            return BitConverter.GetBytes(ComputHash(array,seed, FnvOffsetBasis, ModuloValue));
        }

        public byte[] Hash(byte[] array, ulong fnvPrime, ulong offset, ulong modulo)
        {
            return BitConverter.GetBytes(ComputHash(array, fnvPrime, offset, modulo));
        }

        private static ulong ComputHash(byte[] array, ulong fnvPrime, ulong offset, ulong modulo)
        {
            if (fnvPrime == 0)
            {
                fnvPrime = FnvPrime;
            }
            if (modulo == 0)
            {
                modulo = ModuloValue;
            }
            var hash = offset;
            unchecked
            {
                for (int index = 0; index < array.Length; index++)
                {
                    hash ^= array[index];
                    hash = hash*fnvPrime%modulo;                   
                }
            }
            return hash;
        }
    }
}
