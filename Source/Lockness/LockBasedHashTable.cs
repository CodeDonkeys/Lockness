using System;
using System.Collections;
using System.Collections.Generic;

namespace CodeDonkeys.Lockness
{
    internal class HashTableComparer<TElement> : IEqualityComparer<TElement>
    {
        private readonly IComparer<TElement> internalComperer;
        public HashTableComparer(IComparer<TElement> elementComparer)
        {
            internalComperer = elementComparer;
        }

        public bool Equals(TElement x, TElement y)
        {
            return internalComperer.Compare(x, y) == 0;
        }

        public int GetHashCode(TElement obj)
        {
            return obj.GetHashCode();
        }
    }
    public sealed class LockBasedHashTable<TElement> : ISet<TElement>
    {
        private readonly HashSet<TElement> list;
        public LockBasedHashTable(IComparer<TElement> elementComparer)
        {
            list = new HashSet<TElement>(new HashTableComparer<TElement>(elementComparer));
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
