using System;
using System.Collections.Generic;


namespace TBag.BloomFilters
{
    public class HighUtilizationCountConfiguration : ICountConfiguration<int>
    {
        public Func<int, int> CountDecrease { get; set; } = i => i- 1;

        public Func<int> CountIdentity { get; set; } = ()=>0;

        public Func<int, int> CountIncrease { get; set; } = i => i + 1;

        public Func<int, int, int> CountSubtract { get; set; } = (i1,i2)=>i1-i2;

        public Func<int> CountUnity { get; set; } = ()=>1;

        public IEqualityComparer<int> EqualityComparer { get; set; } = EqualityComparer<int>.Default;

        public Func<int, bool> IsPureCount { get; set; } = i => Math.Abs(i) == 1;
    }
}
