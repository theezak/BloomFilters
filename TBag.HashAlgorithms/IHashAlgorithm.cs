namespace TBag.HashAlgorithms
{
    /// <summary>
    ///     Hash algorithms for Couchbase.
    /// </summary>
    public interface IHashAlgorithm
    {
        byte[] Hash(byte[] array, uint seed = 0);
    }
}