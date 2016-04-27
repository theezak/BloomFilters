namespace System.Collections.Generic
{
    using Diagnostics.Contracts;
    using Linq;

    /// <summary>
    /// Extensions for hashsets.
    /// </summary>
    internal static class HashSetExtensions
    {
        /// <summary>
        /// Move modified keys to the modified list.
        /// </summary>
        /// <typeparam name="TId">Type of the key</typeparam>
        /// <param name="modifiedEntities">The modified entities</param>
        /// <param name="listA">Identifiers only in the first set</param>
        /// <param name="listB">Identifiers only in the second set</param>
        internal static void MoveModified<TId>(this HashSet<TId> modifiedEntities, HashSet<TId> listA, HashSet<TId> listB)
        {
            Contract.Requires(listA != null);
            Contract.Requires(listB != null);
            if (listA == modifiedEntities || listB == modifiedEntities) return;
            foreach (var modItem in listA.Where(listB.Contains).ToArray())
            {
                modifiedEntities.Add(modItem);
            }
            foreach (var modItem in modifiedEntities)
            {
                listA.Remove(modItem);
                listB.Remove(modItem);
            }
        }
    }
}
