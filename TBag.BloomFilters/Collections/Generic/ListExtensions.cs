namespace System.Collections.Generic
{
    using Diagnostics.Contracts;
    using Linq;

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
            Contract.Requires(list != null);
            return Enumerable
                .Range(0, 1 << list.Count)
                .Select(m => Enumerable.Range(0, list.Count).Where(i => (m & (1 << i)) != 0).Select(i => list[i]));
        }
    }
}
