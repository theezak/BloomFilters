using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBag.HashAlgorithms
{
    public interface IFnvHash32 : IHashAlgorithm
    {
        byte[] Hash(byte[] array, uint fnvPrime, uint offset, uint modulo);
    }
}
