using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBag.BloomFilters
{
    public class CountConfiguration : ICountConfiguration<sbyte>
    {
        public Func<sbyte, sbyte> CountDecrease { get; set; } =  sb => (sbyte)(sb - 1);

        public Func<sbyte> CountIdentity { get; set; } = ()=>0;

        public Func<sbyte, sbyte> CountIncrease { get; set; } = sb => (sbyte)(sb + 1);

        public Func<sbyte, sbyte, sbyte> CountSubtract { get; set; } = (sb1,sb2) => (sbyte)(sb1 - sb2);

        public Func<sbyte> CountUnity { get; set; } = ()=>1;

        public IEqualityComparer<sbyte> EqualityComparer { get; set; } = EqualityComparer<sbyte>.Default;

        public Func<sbyte, bool> IsPureCount { get; set; } = sb => Math.Abs(sb) == 1;
    }
}
