namespace TBag.HashAlgorithms
{
    public interface IFnvHash64 : IHashAlgorithm
    {
        byte[] Hash(byte[] array, ulong fnvPrime, ulong offset, ulong modulo);
    }
}
