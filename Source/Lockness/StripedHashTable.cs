using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CodeDonkeys.Lockness
{
    public class StripedHashTable<TElement> : ISet<TElement>
    {
        private readonly IComparer<TElement> elementComparer;
        private readonly double maxFilledRatio;
        private volatile List<TElement>[] table;
        private object[] lockObects;
        private volatile int size;
        private volatile int filledCount;

        public StripedHashTable(IComparer<TElement> elementComparer) : this(elementComparer, 0.8, 10)
        {
        }

        public StripedHashTable(IComparer<TElement> elementComparer, double maxFilledRatio, int initSize)
        {
            this.elementComparer = elementComparer;
            this.maxFilledRatio = maxFilledRatio;
            size = initSize;
            lockObects = Enumerable.Range(0, size).Select(x => new object()).ToArray();
            table = Enumerable.Range(0, size).Select(x => new List<TElement>()).ToArray();
            filledCount = 0;
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            return new Enumerator(table);
        }

        public bool Add(TElement element)
        {
            var hash = GetHash(element);
            try
            {
                Monitor.Enter(lockObects[hash]);
                if (FindInOneHashLine(hash, element))
                {
                    return false;
                }
                table[hash].Add(element);
                if (table[hash].Count == 1)
                {
                    filledCount++;
                }
            }
            finally
            {
                Monitor.Exit(lockObects[hash]);
            }

            if ((double) filledCount / size > maxFilledRatio)
            {
                Resize();
            }

            return true;
        }

        public bool Contains(TElement element)
        {
            var hash = GetHash(element);
            return FindInOneHashLine(hash, element);
        }

        public bool Remove(TElement element)
        {
            var hash = GetHash(element);
            try
            {
                Monitor.Enter(lockObects[hash]);
                if (!FindInOneHashLine(hash, element))
                {
                    return false;
                }
                table[hash].Remove(element);
            }
            finally
            {
                Monitor.Exit(lockObects[hash]);
            }

            return true;
        }

        private void Resize()
        {
            var oldLockObjects = lockObects;
            try
            {
                foreach (var lockObect in oldLockObjects)
                {
                    Monitor.Enter(lockObect);
                }
                if (table.Length != size)
                {
                    return;
                }
                size = size * 2;
                filledCount = 0;
                var newTable = Enumerable.Range(0, size).Select(x => new List<TElement>()).ToArray();
                foreach (var list in table)
                {
                    if (list == null)
                    {
                        continue;
                    }
                    foreach (var element in list)
                    {
                        var hash = GetHash(element);
                        newTable[hash].Add(element);
                        if (newTable[hash].Count == 1)
                        {
                            filledCount++;
                        }
                    }
                }
                table = newTable;

                lockObects = Enumerable.Range(0, size).Select(x => new object()).ToArray();
            }
            finally
            {
                foreach (var lockObect in oldLockObjects)
                {
                    Monitor.Exit(lockObect);
                }
            }
        }

        private int GetHash(TElement element)
        {
            return element.GetHashCode() % size;
        }

        private bool FindInOneHashLine(long hash, TElement element)
        {
            return table[hash].Any(x => elementComparer.Compare(x, element) == 0);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private struct Enumerator : IEnumerator<TElement>
        {
            public TElement Current => currentList != -1 ? table[currentList][curentElement] : default(TElement);
            object IEnumerator.Current => Current;

            private readonly List<TElement>[] table;
            private int currentList;
            private int curentElement;

            public Enumerator(List<TElement>[] table) : this()
            {
                this.table = table;
                curentElement = -1;
                currentList = GetNextFilledList(0);
            }

            private int GetNextFilledList(int startList)
            {
                var i = startList;
                while (i < table.Length)
                {
                    if (table[i].Count > 0)
                    {
                        return i;
                    }
                    i++;
                }
                return -1;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (currentList == -1)
                {
                    return false;
                }
                if (table[currentList].Count - 1 == curentElement)
                {
                    curentElement = 0;
                    currentList = GetNextFilledList(currentList + 1);
                }
                else
                {
                    curentElement++;
                }
                return currentList != -1;
            }

            public void Reset()
            {
                currentList = GetNextFilledList(0);
                curentElement = 0;
            }
        }
    }
}