using System;
using System.Collections.Generic;

namespace GenesisUI.Collections
{
    /// <summary>List orderings the views need, without allocating per call more than one buffer.</summary>
    public static class ListOrder
    {
        /// <summary>
        /// Moves the items matching <paramref name="first"/> to the front, keeping the order inside each
        /// group. Index-based: a foreach that writes back into the list throws "Collection was modified"
        /// (R-058: the crafting window faulted on every opening with craftable and non-craftable recipes).
        /// </summary>
        public static void StablePartition<T>(List<T> list, Func<T, bool> first, List<T> buffer)
        {
            buffer.Clear();
            int write = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (first(item)) list[write++] = item;
                else buffer.Add(item);
            }
            for (int i = 0; i < buffer.Count; i++) list[write + i] = buffer[i];
            buffer.Clear();
        }
    }
}
