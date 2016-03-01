using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBag.BloomFilters
{
    public abstract class BloomFilterConfigurationBase<T, THash, TId, TIdHash> :
        BloomFilterIdConfigurationBase<TId, TIdHash>,
        IBloomFilterConfiguration<T, THash, TId, TIdHash>
        where THash : struct
        where TIdHash : struct
    {
        public Func<T, THash> GetEntityHash { get; set; }
        public Func<T, TId> GetId { get; set; }
        public Func<THash, bool> IsHashIdentity { get; set; }
        public Func<THash, THash, THash> EntityHashXor { get; set; }

        public bool SplitByHash
        {
            get; set;
        }

        public bool UseRecurringMinimum
        {
            get; set;
        }

        public float RecurringMinimumSizeFactor
        {
            get; set;
        } = 0.5F;
    }
}

  
