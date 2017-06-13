using System;
using System.Collections;
using System.Collections.Generic;

namespace CodeDonkeys.Lockness
{
    public sealed class LockBasedList<TElement> : ISet<TElement>
    {
        private readonly SortedList<TElement, int> list;
        private readonly object lockObject;
        public LockBasedList(IComparer<TElement> elementComparer)
        {
            list = new SortedList<TElement, int>(elementComparer);
            lockObject = new object();
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            return list.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Add(TElement element)
        {
            lock (lockObject)
            {
                if (!list.ContainsKey(element))
                    return false;
                list.Add(element, 0);
                return true;
            }
        }

        public bool Contains(TElement element)
        {
            lock (lockObject)
            {
                return list.ContainsKey(element);
            }
        }

        public bool Remove(TElement element)
        {
            lock (lockObject)
            {
                return list.Remove(element);
            }
        }
    }
}