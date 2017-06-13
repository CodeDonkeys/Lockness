using System.Collections;
using System.Collections.Generic;

namespace CodeDonkeys.Lockness
{
    internal class LockBasedRedBlackTree<TElement> : ISet<TElement>
    {
        private readonly SortedDictionary<TElement, object> internalSet;
        private IComparer<TElement> elementComparer;
        private readonly object lockObect;

        public LockBasedRedBlackTree(IComparer<TElement> elementComparer)
        {
            this.elementComparer = elementComparer;
            internalSet = new SortedDictionary<TElement, object>(elementComparer);
            lockObect = new object();
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            IEnumerable<TElement> keys;
            lock (lockObect)
            {
                keys = internalSet.Keys;
            }
            return keys.GetEnumerator();
        }

        public bool Add(TElement element)
        {
            lock (lockObect)
            {
                if (!internalSet.ContainsKey(element))
                    return false;
                internalSet.Add(element, null);
                return true;
            }
        }

        public bool Contains(TElement element)
        {
            lock (lockObect)
            {
                return internalSet.ContainsKey(element);
            }
        }

        public bool Remove(TElement element)
        {
            lock (lockObect)
            {
                return internalSet.Remove(element);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}