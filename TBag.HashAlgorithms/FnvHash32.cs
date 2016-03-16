namespace TBag.HashAlgorithms
{
    using System;

    public class FnvHash32 : IFnvHash32
    {
        private const uint FnvPrime = unchecked(16777619);
        private const uint FnvOffsetBasis = unchecked(2166136261);
        private const uint ModuloValue = uint.MaxValue;
        public byte[] Hash(byte[] array, uint seed = 0)
        {
            return BitConverter.GetBytes(ComputHash(array, seed, FnvOffsetBasis, ModuloValue));
        }

        private static uint ComputHash(byte[] array, uint fnvPrime, uint offset, uint moduloValue)
        {
            if (moduloValue == 0)
            {
                moduloValue = uint.MaxValue;
            }
            if (fnvPrime == 0)
            {
                fnvPrime = FnvPrime;
            }
            var hash = offset;
            unchecked
            {
                for (int index = 0; index < array.Length; index++)
                {
                    hash ^= array[index];
                    hash = hash*fnvPrime%moduloValue;
                }
            }
            return hash;
        }

        public byte[] Hash(byte[] array, uint fnvPrime, uint offset, uint modulo)
        {
            return BitConverter.GetBytes(ComputHash(array, fnvPrime, offset, modulo));
        }
    }
}
