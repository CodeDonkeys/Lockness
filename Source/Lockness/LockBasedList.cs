using System;
using System.Collections;
using System.Collections.Generic;

namespace CodeDonkeys.Lockness
{
    public sealed class LockBasedList<TElement> : ISet<TElement>
    {
        private readonly SortedList<TElement, int> list;
        public LockBasedList(IComparer<TElement> elementComparer)
        {
            list = new SortedList<TElement, int>(elementComparer);
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
            try
            {
                list.Add(element, 0);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
            
        }

        public bool Contains(TElement element)
        {
            return list.ContainsKey(element);
        }

        public bool Remove(TElement element)
        {
            return list.Remove(element);
        }
    }
}