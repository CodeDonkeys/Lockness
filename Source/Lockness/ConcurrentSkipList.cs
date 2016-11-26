﻿using System;

namespace CodeDonkeys.Lockness
{
    public class ConcurrentSkipList<TKey, TValue> : IMap<TKey, TValue> where TKey: IComparable
    {
        private readonly int maxLevel;
        private readonly RandomGenerator randomGenerator;
        private readonly ConcurrentSkipListHeadNode<TKey, TValue> head;

        public ConcurrentSkipList(int maxLevel)
        {
            head = InitializeHeadAndTailTower(maxLevel);
            this.maxLevel = maxLevel;
            this.randomGenerator = new RandomGenerator();

        }

        public bool Add(TKey key, TValue value)
        {
            var searchedNodes = SearchToGivenLevel(key, 0);
            var leftNode = searchedNodes.LeftNode;
            if (NodeHasThisKey(key, leftNode))
                return false;
            var newRootNode = new ConcurrentSkipListRootNode<TKey, TValue>(key, value, null);
            ConcurrentSkipListNode<TKey, TValue> currentInsertedNode = newRootNode;
            var currentLevel = 0;
            while (currentLevel < maxLevel && randomGenerator.NeedNextLevel())
            {
                if (!InsertOneNode(currentInsertedNode, searchedNodes))
                {
                    return false;
                }
                currentLevel++;
                currentInsertedNode = new ConcurrentSkipListNode<TKey, TValue>(currentInsertedNode, newRootNode, null);
                searchedNodes = SearchToGivenLevel(key, currentLevel);
            }
            return true;
        }

        public bool Contains(TKey key)
        {
            var searchedNodes = SearchToGivenLevel(key, 0);
            var leftNode = searchedNodes.LeftNode;
            return NodeHasThisKey(key, leftNode);
        }

        public bool Remove(TKey key)
        {
            throw new NotImplementedException();
        }

        private bool NodeHasThisKey(TKey key, ConcurrentSkipListNode<TKey, TValue> node)
        {
            return !(node is ConcurrentSkipListHeadNode<TKey, TValue>) && node.RootNode.Key.CompareTo(key) == 0;
        }

        private bool InsertOneNode(ConcurrentSkipListNode<TKey, TValue> newNode, SearchedNodes nearbyNodes)
        {
            while (true)
            {
                if (NodeHasThisKey(newNode.RootNode.Key, nearbyNodes.LeftNode))
                    return false;
                newNode.NextReference = new AtomicMarkableReference<ConcurrentSkipListNode<TKey, TValue>, SkipListLables>(nearbyNodes.RightNode, SkipListLables.None);
                if (nearbyNodes.LeftNode.NextReference.CompareAndSet(nearbyNodes.RightNode, newNode, SkipListLables.None,
                    SkipListLables.None))
                    return true;
                nearbyNodes = SearchOnOneLevel(newNode.RootNode.Key, nearbyNodes.LeftNode);
            }
        }

        private ConcurrentSkipListHeadNode<TKey, TValue> InitializeHeadAndTailTower(int level)
        {
            //нам неважно, много хвостовых вершин или одна
            var tailNode = new ConcurrentSkipListTailNode<TKey, TValue>();
            var previousNode = new ConcurrentSkipListHeadNode<TKey, TValue>(null, new AtomicMarkableReference<ConcurrentSkipListNode<TKey, TValue>, SkipListLables>(tailNode, SkipListLables.None), null);
            previousNode.UpNode = previousNode;
            while (level > 0)
            {
                var currentNode = new ConcurrentSkipListHeadNode<TKey, TValue>(null, new AtomicMarkableReference<ConcurrentSkipListNode<TKey, TValue>, SkipListLables>(tailNode, SkipListLables.None), previousNode);
                previousNode.DownNode = currentNode;
                previousNode = currentNode;
                level--;
            }
            return previousNode;
        }

        private SearchedNodes SearchToGivenLevel(TKey key, int level)
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

        private SearchedNodes SearchOnOneLevel(TKey key, ConcurrentSkipListNode<TKey, TValue> startNode)
        {
            SkipListLables mark;
            ConcurrentSkipListNode<TKey, TValue> currentNode = startNode;
            var nextNode = currentNode.NextReference.Get(out mark);
            while (nextNode.NextReference != null && nextNode.RootNode.Key.CompareTo(key) <= 0)
            {
                currentNode = nextNode;
                nextNode = currentNode.NextReference.Get(out mark);
            }
            return new SearchedNodes(currentNode, nextNode);
        }

        private NodeOnLevel GetUpperHeadNode()
        {
            var currentNode = head;
            var currentLevel = 0;
            SkipListLables mark;
            while (currentNode.NextReference.Get(out mark).NextReference != null)
            {
                currentNode = currentNode.UpNode;
                currentLevel++;
            }
            return new NodeOnLevel(currentLevel, currentNode);
        }

        private class RandomGenerator
        {
            private Random rand;

            public RandomGenerator()
            {
                rand = new Random();
            }

            public bool NeedNextLevel()
            {
                return rand.Next(2) == 1;
            }
        }

        private class NodeOnLevel
        {
            public ConcurrentSkipListHeadNode<TKey, TValue> Node { get; set; }
            public int Level { get; set; }

            public NodeOnLevel(int level, ConcurrentSkipListHeadNode<TKey, TValue> node)
            {
                Level = level;
                Node = node;
            }
        }

        private class SearchedNodes
        {
            public ConcurrentSkipListNode<TKey, TValue> LeftNode { get; set; }
            public ConcurrentSkipListNode<TKey, TValue> RightNode { get; set; }

            public SearchedNodes(ConcurrentSkipListNode<TKey, TValue> leftNode, ConcurrentSkipListNode<TKey, TValue> rightNode)
            {
                LeftNode = leftNode;
                RightNode = rightNode;
            }
        }
    }
}