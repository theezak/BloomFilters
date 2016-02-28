using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBag.BloomFilters
{
    public abstract class BloomFilterIdConfigurationBase<TId, THash>
       where THash : struct
    {
        public Func<TId, uint, IEnumerable<THash>> IdHashes { get; set; }

        public Func<TId, TId, TId> IdXor { get; set; }

        public Func<TId, bool> IsIdIdentity { get; set; }

        public Func<THash, bool> IsIdHashIdentity { get; set; }

    }
}