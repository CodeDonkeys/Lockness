using System;
using System.Collections;
using System.Collections.Generic;

namespace CodeDonkeys.Lockness
{
    public sealed class LockBasedHashTable<TElement> : ISet<TElement>
    {
        private readonly HashSet<TElement> list;
        public LockBasedHashTable(IEqualityComparer<TElement> elementComparer)
        {
            list = new HashSet<TElement>(elementComparer);
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Add(TElement element)
        {
            try
            {
                list.Add(element);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }

        }

        public bool Contains(TElement element)
        {
            return list.Contains(element);
        }

        public bool Remove(TElement element)
        {
            return list.Remove(element);
        }
    }
}
