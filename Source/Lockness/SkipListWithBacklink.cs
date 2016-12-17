using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace CodeDonkeys.Lockness
{
    //TODO Нет тестов, которые хотя бы проверяли в однопоточном сценарии
    public class SkipListWithBacklink<TElement> : ISet<TElement> where TElement: IComparable
    {
        private readonly int maxLevel;
        private readonly RandomGenerator randomGenerator;
        private readonly SkipListHeadNodeWithBacklink<TElement> head;
        private readonly AtomicMarkableReferenceBuilder<SkipListNodeWithBacklink<TElement>, SkipListLables> AMRBuilder;

        public SkipListWithBacklink(int maxLevel)
        {
            head = InitializeHeadAndTailTower(maxLevel);
            this.maxLevel = maxLevel;
            randomGenerator = new RandomGenerator();
            AMRBuilder = new AtomicMarkableReferenceBuilder<SkipListNodeWithBacklink<TElement>, SkipListLables>();
        }

        public bool Add(TElement element)
        {
            var searchedNodes = SearchToGivenLevel(element, 0);
            var leftNode = searchedNodes.LeftNode;
            if (leftNode.ElementIsEqualsTo(element))
                return false;
            var newRootNode = new SkipListRootNodeWithBacklink<TElement>(element, null);
            var currentInsertedNode = (SkipListNodeWithBacklink<TElement>)newRootNode;
            var currentLevel = 0;
            var levelsCount = randomGenerator.GetLevelsCount(maxLevel);
            while (currentLevel < levelsCount)
            {
                if (!InsertOneNode(currentInsertedNode, searchedNodes))
                {
                    return false;
                }
                currentLevel++;
                currentInsertedNode = new SkipListNodeWithBacklink<TElement>(currentInsertedNode, newRootNode, AMRBuilder.Empty());
                searchedNodes = SearchToGivenLevel(element, currentLevel);
            }
            return true;
        }

        public bool Contains(TElement element)
        {
            var searchedNodes = SearchToGivenLevel(element, 0);
            var leftNode = searchedNodes.LeftNode;
            return leftNode.ElementIsEqualsTo(element);
        }

        public bool Remove(TElement element)
        {
            // не уверена, что это лучший способ...
            var searchedNodes = SearchToGivenLevel(element, 1);
            var deletedNode = searchedNodes.RightNode;
            if (deletedNode.RootNode.Element.CompareTo(element) != 0)
                return false;
            var previousNode = SearchToGivenLevel(searchedNodes.LeftNode.RootNode.Element, 1).LeftNode;
            if (DeleteOneNode(previousNode, deletedNode))
                return false;
            SearchToGivenLevel(element, 2);
            return true;
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            return new Enumerator(head.RootNode);
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
                if (previousNode.ElementIsEqualsTo(newNode.RootNode.Element))
                    return false;
                newNode.NextReference = AMRBuilder.Build(nearbyNodes.RightNode, SkipListLables.None);
                if (previousNode.NextReference.CompareAndSet(nearbyNodes.RightNode, newNode, SkipListLables.None, SkipListLables.None))
                    return true;
                else
                {
                    SkipListLables oldLable;
                    nearbyNodes.RightNode.NextReference.Get(out oldLable);
                    if (oldLable.HasLable(SkipListLables.Flag))
                        DeleteOneNode(nearbyNodes.RightNode, nearbyNodes.RightNode.NextReference);
                    while (oldLable.HasLable(SkipListLables.Mark))
                        previousNode = previousNode.Backlink;
                }
                nearbyNodes = SearchOnOneLevel(newNode.RootNode.Element, previousNode);
                spin.SpinOnce();
            }
        }

        private SkipListHeadNodeWithBacklink<TElement> InitializeHeadAndTailTower(int level)
        {
            //нам неважно, много хвостовых вершин или одна
            var tailNode = new SkipListTailNodeWithBacklink<TElement>();
            var previousNode = new SkipListHeadNodeWithBacklink<TElement>(null, AMRBuilder.Build(tailNode, SkipListLables.None), null);
            previousNode.UpNode = previousNode;
            while (level > 0)
            {
                var currentNode = new SkipListHeadNodeWithBacklink<TElement>(null, AMRBuilder.Build(tailNode, SkipListLables.None), previousNode);
                previousNode.DownNode = currentNode;
                previousNode = currentNode;
                level--;
            }
            return previousNode;
        }

        private SearchedNodes SearchToGivenLevel(TElement key, int level)
        {
            var nodeOnLevel = GetUpperHeadNode();
            var currentLevel = nodeOnLevel.Level;
            var searchedNodes = SearchOnOneLevel(key, nodeOnLevel.Node);
            while (currentLevel > level)
            {
                searchedNodes = SearchOnOneLevel(key, searchedNodes.LeftNode.DownNode);
                currentLevel--;
            }
            return searchedNodes;
        }

        private SearchedNodes SearchOnOneLevel(TElement key, SkipListNodeWithBacklink<TElement> startNode)
        {
            SkipListNodeWithBacklink<TElement> currentNode = startNode;
            SkipListNodeWithBacklink<TElement> nextNode = currentNode.NextReference;
            while (nextNode.NextReference != null && nextNode.RootNode.Element.CompareTo(key) <= 0)
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

                if (SearchOnOneLevel(deletedNode.RootNode.Element, previousNode).LeftNode != deletedNode)
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

        //XORShift
        private class RandomGenerator
        {
            private long state;

            public RandomGenerator() : this(DateTime.Now.Ticks) {}

            public RandomGenerator(long seed)
            {
                state = seed;
            }

            public int GetLevelsCount(int maxValue)
            {
                long newState;
                long oldState;
                do
                {
                    oldState = state;
                    newState = oldState;
                    newState ^= (newState << 21);
                    newState ^= (newState >> 35);
                    newState ^= (newState << 4);
                } while (Interlocked.CompareExchange(ref state, newState, oldState) == oldState);
                var result = (int) (state % maxValue);
                return (result < 0) ? -result : result;
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
            private readonly SkipListRootNodeWithBacklink<TElement> head;
            private SkipListNodeWithBacklink<TElement> current;

            public TElement Current => current.RootNode.Element;

            object IEnumerator.Current => current.RootNode.Element;

            internal Enumerator(SkipListRootNodeWithBacklink<TElement> head)
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