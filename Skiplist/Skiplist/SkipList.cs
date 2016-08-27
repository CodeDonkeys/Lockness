using System;

namespace Skiplist
{
    public class SkipListNode<TKey, TValue> where TKey: IComparable
    {
        public TKey Key { get; set; }
        public TValue Value { get; set; }
        public SkipListNode<TKey, TValue> Next { get; set; }

        public SkipListNode(TKey key, TValue value, SkipListNode<TKey, TValue> next)
        {
            Key = key;
            Value = value;
            Next = next;
        }
    }

    public class SkipListLane<TKey, TValue> where TKey : IComparable
    {
        public SkipListLane<TKey, TValue> Next { get; set; }
        public SkipListLane<TKey, TValue> Down { get; set; }
        public SkipListNode<TKey, TValue> Node { get; set; }

        public SkipListLane(SkipListLane<TKey, TValue> next, SkipListLane<TKey, TValue> down, SkipListNode<TKey, TValue> node)
        {
            Next = next;
            Down = down;
            Node = node;
        }
    }

    public class SkipList<TKey, TValue> where TKey : IComparable
    {
        protected SkipListLane<TKey, TValue> firstLane;
        private Random random;

        public SkipList()
        {
            random = new Random(); 
        }

        public void Add(TKey key, TValue value)
        {
            if (firstLane == null)
            {
                firstLane = new SkipListLane<TKey, TValue>(null, null, new SkipListNode<TKey, TValue>(key, value, null));
                return;
            }
            if (firstLane.Node.Key.CompareTo(key) > 0)
            {
                var newNode = new SkipListNode<TKey, TValue>(key, value, firstLane.Node);
                firstLane = new SkipListLane<TKey, TValue>(firstLane, null, newNode);
                var currentLane = firstLane;
                while (currentLane.Next.Down != null)
                {
                    var newLane = new SkipListLane<TKey, TValue>(currentLane.Next.Down, null, newNode);
                    currentLane.Down = newLane;
                    currentLane = newLane;
                }
                if (random.Next(0, 3) == 0)
                    firstLane = new SkipListLane<TKey, TValue>(null, firstLane, firstLane.Node);
                return;
            }
            TryInternalAdd(key, value, firstLane);
        }

        private SkipListLane<TKey, TValue> TryInternalAdd(TKey key, TValue value, SkipListLane<TKey, TValue> currentLane)
        {
            while (true)
            {
                var nextLane = currentLane.Next;
                SkipListLane<TKey, TValue> nodeOnNextStep = null;

                if (nextLane == null)
                {
                    if (currentLane.Down == null)
                    {
                        return AddNewLane(key, value, currentLane);
                    }
                    nodeOnNextStep = TryInternalAdd(key, value, currentLane.Down);
                }
                else
                {
                    var compareResult = nextLane.Node.Key.CompareTo(key);

                    if (compareResult < 0)
                    {
                        currentLane = nextLane;
                        continue;
                    }

                    if (compareResult > 0)
                    {
                        if (currentLane.Down == null)
                        {
                            return AddNewLane(key, value, currentLane);
                        }
                        nodeOnNextStep = TryInternalAdd(key, value, currentLane.Down);
                    }

                    if (compareResult == 0)
                    {
                        nextLane.Node.Value = value;
                        return null;
                    }
                }
                if (nodeOnNextStep == null)
                    return null;
                if (random.Next(0, 3) > 0)
                    return null;
                return CreateAndAddNewLane(currentLane, nodeOnNextStep, nodeOnNextStep.Node);
            }
        }

        private SkipListLane<TKey, TValue> AddNewLane(TKey key, TValue value, SkipListLane<TKey, TValue> currentLane)
        {
            var newNode = new SkipListNode<TKey, TValue>(key, value, currentLane.Node.Next);
            currentLane.Node.Next = newNode;
            return CreateAndAddNewLane(currentLane, null, newNode);
        }

        private SkipListLane<TKey, TValue> CreateAndAddNewLane(SkipListLane<TKey, TValue> previousLane, SkipListLane<TKey, TValue> downLane, SkipListNode<TKey, TValue> node)
        {
            var newLane = new SkipListLane<TKey, TValue>(previousLane.Next, downLane, node);
            previousLane.Next = newLane;
            return newLane;
        }

        public void Delete(TKey key)
        {
            if (firstLane == null)
                return;
            var firstLaneCompare = firstLane.Node.Key.CompareTo(key);
            if (firstLaneCompare > 0)
                return;
            var currentLane = firstLane;
            if (firstLaneCompare == 0)
            {
                while (currentLane.Next.Node.Key.CompareTo(currentLane.Node.Next.Key) != 0)
                {
                    currentLane = currentLane.Down;
                }
                firstLane = currentLane.Next;
                return;
            }
            while (currentLane != null)
            {
                while (currentLane?.Next?.Node.Key.CompareTo(key) < 0)
                {
                    currentLane = currentLane.Next;
                }
                if (currentLane?.Next?.Node.Key.CompareTo(key) == 0)
                {
                    currentLane.Next = currentLane.Next.Next;
                    if (currentLane.Down == null)
                    {
                        currentLane.Node.Next = currentLane.Node.Next.Next;
                    }
                }
                currentLane = currentLane.Down;
            }
        }

        public TValue Search(TKey key)
        {
            if (firstLane == null)
                return default(TValue);

            var currentLane = firstLane;

            while (true)
            {
                var nextLane = currentLane.Next;
                if (nextLane == null)
                    return default(TValue);

                var compareResult = nextLane.Node.Key.CompareTo(key);

                if (compareResult == 0)
                {
                    return nextLane.Node.Value;
                }

                if (compareResult > 0)
                {
                    if (currentLane.Down == null)
                        return default(TValue);
                    currentLane = currentLane.Down;
                    continue;
                }

                if (compareResult < 0)
                {
                    currentLane = nextLane;
                }
            }
        }
    }
}