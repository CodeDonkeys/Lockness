using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace CodeDonkeys.Lockness
{
    public sealed class SkipListWithBacklink<TElement> : ISet<TElement>
    {
        private readonly IComparer<TElement> comparer;
        private readonly int maxLevel;
        private readonly RandomGenerator randomGenerator;
        private readonly SkipListHeadNodeWithBacklink<TElement> head;

        public SkipListWithBacklink(IComparer<TElement> elementComparer, int maxLevel = 32)
        {
            this.maxLevel = maxLevel;
            comparer = elementComparer;
            head = InitializeHeadAndTailTower(this.maxLevel);
            randomGenerator = new RandomGenerator();
        }

        public bool Add(TElement element)
        {
            var nearbyNodes = SearchToGivenLevel(element, 0, SearchStopCondition.LessOrEqual);
            var leftNode = nearbyNodes.LeftNode;
            if (leftNode.ElementIsEqualsTo(element, comparer))
                return false;
            var newRootNode = new SkipListRootNodeWithBacklink<TElement>(element, null);
            var currentInsertedNode = (SkipListNodeWithBacklink<TElement>)newRootNode;
            var currentLevel = 0;
            var levelsCount = randomGenerator.GetLevelsCount(maxLevel);
            while (currentLevel < levelsCount)
            {
                if (!InsertOneNode(currentInsertedNode, nearbyNodes))
                {
                    return false;
                }
                currentLevel++;
                currentInsertedNode = new SkipListNodeWithBacklink<TElement>(currentInsertedNode, newRootNode, AtomicMarkableReference<SkipListNodeWithBacklink<TElement>, SkipListLables>.Empty());
                nearbyNodes = SearchToGivenLevel(element, currentLevel, SearchStopCondition.LessOrEqual);
            }
            return true;
        }

        public bool Contains(TElement element)
        {
            var nearbyNodes = SearchToGivenLevel(element, 0, SearchStopCondition.LessOrEqual);
            var leftNode = nearbyNodes.LeftNode;
            return leftNode.ElementIsEqualsTo(element, comparer);
        }

        public bool Remove(TElement element)
        {
            var nearbyNodes = SearchToGivenLevel(element, 0, SearchStopCondition.Less);
            var deletedNode = nearbyNodes.RightNode;
            if (deletedNode is SkipListHeadNodeWithBacklink<TElement> || deletedNode is SkipListTailNodeWithBacklink<TElement> || comparer.Compare(deletedNode.RootNode.Element, element) != 0)
                return false;
            var previousNode = nearbyNodes.LeftNode;
            if (!DeleteOneNode(previousNode, deletedNode))
                return false;
            SearchToGivenLevel(element, 1, SearchStopCondition.Less);
            return true;
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            return new Enumerator(head);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private bool InsertOneNode(SkipListNodeWithBacklink<TElement> newNode, SearchedNodes nearbyNodes)
        {
            var spin = new SpinWait();
            while (true)
            {
                var previousNode = nearbyNodes.LeftNode;
                if (previousNode.ElementIsEqualsTo(newNode.RootNode.Element, comparer))
                    return false;
                newNode.NextReference = AtomicMarkableReference<SkipListNodeWithBacklink<TElement>, SkipListLables>.New(nearbyNodes.RightNode, SkipListLables.None);
                if (previousNode.NextReference.CompareAndSet(nearbyNodes.RightNode, newNode, SkipListLables.None, SkipListLables.None))
                    return true;

                SkipListLables oldLable;
                if (!(previousNode.NextReference.Get(out oldLable) is SkipListTailNodeWithBacklink<TElement>))
                    while (oldLable.HasLable(SkipListLables.Mark))
                    {
                        previousNode = previousNode.Backlink;
                        previousNode.NextReference.Get(out oldLable);
                    }

                nearbyNodes = SearchOnOneLevel(newNode.RootNode.Element, previousNode, SearchStopCondition.LessOrEqual);
                spin.SpinOnce();
            }
        }

        private SkipListHeadNodeWithBacklink<TElement> InitializeHeadAndTailTower(int level)
        {
            //нам неважно, много хвостовых вершин или одна
            var tailNode = new SkipListTailNodeWithBacklink<TElement>();
            var previousNode = new SkipListHeadNodeWithBacklink<TElement>(null, AtomicMarkableReference<SkipListNodeWithBacklink<TElement>, SkipListLables>.New(tailNode, SkipListLables.None), null);
            previousNode.UpNode = previousNode;
            while (level > 0)
            {
                var currentNode = new SkipListHeadNodeWithBacklink<TElement>(null, AtomicMarkableReference<SkipListNodeWithBacklink<TElement>, SkipListLables>.New(tailNode, SkipListLables.None), previousNode);
                previousNode.DownNode = currentNode;
                previousNode = currentNode;
                level--;
            }
            return previousNode;
        }

        private SearchedNodes SearchToGivenLevel(TElement key, int level, SearchStopCondition searchStopCondition)
        {
            var nodeOnLevel = GetUpperHeadNode();
            var currentLevel = nodeOnLevel.Level;
            var nearbyNodes = SearchOnOneLevel(key, nodeOnLevel.Node, searchStopCondition);
            while (currentLevel > level)
            {
                nearbyNodes = SearchOnOneLevel(key, nearbyNodes.LeftNode.DownNode, searchStopCondition);
                currentLevel--;
            }
            return nearbyNodes;
        }

        private SearchedNodes SearchOnOneLevel(TElement key, SkipListNodeWithBacklink<TElement> startNode, SearchStopCondition searchStopCondition)
        {
            var stopCondition = searchStopCondition == SearchStopCondition.Less
                ? new Func<TElement, TElement, bool>((key1, key2) => comparer.Compare(key1, key2) < 0)
                : new Func<TElement, TElement, bool>((key1, key2) => comparer.Compare(key1, key2) <= 0);
            SkipListNodeWithBacklink <TElement> currentNode = startNode;
            SkipListNodeWithBacklink<TElement> nextNode = currentNode.NextReference;
            while (nextNode.NextReference != null && stopCondition(nextNode.RootNode.Element, key))
            {
                SkipListLables rootNodeLable;
                nextNode.RootNode.NextReference.Get(out rootNodeLable);
                while (rootNodeLable.HasLable(SkipListLables.Mark))
                {
                    DeleteOneNode(currentNode, nextNode);
                    nextNode = currentNode.NextReference;
                }
                currentNode = nextNode;
                nextNode = currentNode.NextReference;
            }
            return new SearchedNodes(currentNode, nextNode);
        }

        private NodeOnLevel GetUpperHeadNode()
        {
            var currentNode = head;
            var currentLevel = 0;
            SkipListNodeWithBacklink<TElement> nextNode = currentNode.NextReference;
            while (!(nextNode is SkipListTailNodeWithBacklink<TElement>))
            {
                currentNode = currentNode.UpNode;
                currentLevel++;
                nextNode = currentNode.NextReference;
            }
            return new NodeOnLevel(currentLevel, currentNode);
        }

        private bool DeleteOneNode(SkipListNodeWithBacklink<TElement> previousNode, SkipListNodeWithBacklink<TElement> deletedNode)
        {
            if (!TrySetFlagLable(ref previousNode, deletedNode))
                return false;
            SetBacklinkAndMarkLable(previousNode, deletedNode);
            TryPhysicallyDeleteNode(previousNode, deletedNode);
            return true; 
        }

        private bool TrySetFlagLable(ref SkipListNodeWithBacklink<TElement> previousNode, SkipListNodeWithBacklink<TElement> deletedNode)
        {
            var spin = new SpinWait();
            while (true)
            {
                SkipListLables oldLable;
                var nextNode = previousNode.NextReference.Get(out oldLable);
                if (oldLable.HasLable(SkipListLables.Flag))
                    return true;
                if (previousNode.NextReference.CompareAndSet(nextNode, nextNode, SkipListLables.None, SkipListLables.Flag))
                    return true;

                SkipListLables currentLable;
                previousNode.NextReference.Get(out currentLable);
                while (currentLable.HasLable(SkipListLables.Mark))
                {
                    previousNode = previousNode.Backlink;
                    previousNode.NextReference.Get(out currentLable);
                }

                if (SearchOnOneLevel(deletedNode.RootNode.Element, previousNode, SearchStopCondition.LessOrEqual).LeftNode != deletedNode)
                    return false;

                spin.SpinOnce();
            }
        }

        private void SetBacklinkAndMarkLable(SkipListNodeWithBacklink<TElement> previousNode, SkipListNodeWithBacklink<TElement> deletedNode)
        {
            deletedNode.Backlink = previousNode;
            var spin = new SpinWait();
            while (true)
            {
                SkipListLables oldLable;
                var nextNode = deletedNode.NextReference.Get(out oldLable);
                if (oldLable.HasLable(SkipListLables.Mark))
                    return;
                if (deletedNode.NextReference.CompareAndSet(nextNode, nextNode, SkipListLables.None, SkipListLables.Mark))
                    return;
                spin.SpinOnce();
            }
        }

        private void TryPhysicallyDeleteNode(SkipListNodeWithBacklink<TElement> previousNode, SkipListNodeWithBacklink<TElement> deletedNode)
        {
            SkipListLables oldLable;
            var nextNode = deletedNode.NextReference.Get(out oldLable);
            previousNode.NextReference.CompareAndSet(deletedNode, nextNode, SkipListLables.Flag, SkipListLables.None);
        }

        private enum SearchStopCondition
        {
            Less,
            LessOrEqual
        }

        //XORShift
        private class RandomGenerator
        {
            private volatile int state;

            public RandomGenerator() : this(42) {}

            public RandomGenerator(int seed)
            {
                state = seed;
            }

            public int GetLevelsCount(int maxValue)
            {
                int newState;
                int oldState;
                do
                {
                    oldState = state;
                    newState = oldState;
                    newState ^= newState << 13;
                    newState ^= newState >> 17;
                    newState ^= newState << 5;
                } while (Interlocked.CompareExchange(ref state, newState, oldState) != oldState);
                return Math.Abs(newState % maxValue);
            }
        }

        private class NodeOnLevel
        {
            public SkipListHeadNodeWithBacklink<TElement> Node { get; }
            public int Level { get; }

            public NodeOnLevel(int level, SkipListHeadNodeWithBacklink<TElement> node)
            {
                Level = level;  
                Node = node;
            }
        }

        private class SearchedNodes
        {
            public SkipListNodeWithBacklink<TElement> LeftNode { get; }
            public SkipListNodeWithBacklink<TElement> RightNode { get; }

            public SearchedNodes(SkipListNodeWithBacklink<TElement> leftNode, SkipListNodeWithBacklink<TElement> rightNode)
            {
                LeftNode = leftNode;
                RightNode = rightNode;
            }
        }
        private struct Enumerator : IEnumerator<TElement>
        {
            private readonly SkipListNodeWithBacklink<TElement> head;
            private SkipListNodeWithBacklink<TElement> current;

            public TElement Current => current.RootNode.Element;

            object IEnumerator.Current => current.RootNode.Element;

            internal Enumerator(SkipListNodeWithBacklink<TElement> head)
            {
                this.head = head;
                current = head;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                current = current.NextReference;
                return !(current is SkipListTailNodeWithBacklink<TElement>);
            }

            void IEnumerator.Reset()
            {
                current = head;
            }
        }

    }
}