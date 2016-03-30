namespace TBag.BloomFilters.Collections.Generics
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// List extensions
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Generate the powerset of the list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
       public static IEnumerable<IEnumerable<T>> GetPowerSet<T>(this IList<T> list)
        {
            return from m in Enumerable.Range(0, 1 << list.Count)
                   select
                       from i in Enumerable.Range(0, list.Count)
                       where (m & (1 << i)) != 0
                       select list[i];
        }
    }
}
