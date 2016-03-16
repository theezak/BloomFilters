namespace TBag.HashAlgorithms
{
    public interface IFnvHash32 : IHashAlgorithm
    {
        byte[] Hash(byte[] array, uint fnvPrime, uint offset, uint modulo);
    }
}
