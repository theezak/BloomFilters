using System;
namespace TBag.BloomFilters
{
    using System.Collections.Generic;

    public interface IBloomFilterConfiguration<T, THash, TId, TIdHash>
       where THash : struct
      where TIdHash : struct
    {
        Func<T, THash> GetEntityHash { get; set; }

        Func<TId, uint, IEnumerable<TIdHash>> IdHashes { get; set; }

        Func<TId, TId, TId> IdXor { get; set; }

        Func<TId, bool> IsIdIdentity { get; set; }

        Func<T, TId> GetId { get; set; }

        Func<THash, bool> IsHashIdentity { get; set; }

        Func<TIdHash, bool> IsIdHashIdentity { get; set; }
        Func<THash, THash, THash> EntityHashXor { get; set; }

        /// <summary>
        /// When true, each hashed ID will go to its own storage.
        /// </summary>
        bool SplitByHash { get; set; }

        bool UseRecurringMinimum { get; set; }

        float RecurringMinimumSizeFactor { get; set; }
    }

}
