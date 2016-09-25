using System;
using System.Threading;

namespace Skiplist
{
    public class MadLinkedList<TKey, TValue> where TKey : IComparable
    {
        public class State
        {
            public LinkedListNode Next;
            public bool IsMarked;
            public bool IsFlagged;
            public LinkedListNode Backlink;

            public State(LinkedListNode next, bool isMarked, bool isFlagged)
            {
                Next = next;
                IsMarked = isMarked;
                IsFlagged = isFlagged;
            }
        }
        public class Reference
        {
            public State State;

            public Reference(State state)
            {
                State = state;
            }
        }
        public class LinkedListNode
        {
            public readonly TKey Key;
            public TValue Value;
            public Reference NextReference;

            public LinkedListNode(TKey key, TValue value, Reference nextReference)
            {
                Key = key;
                Value = value;
                NextReference = nextReference;
            }

            public bool IsMarked()
            {
                if (NextReference == null)
                    return false;
                return NextReference.State.IsMarked;
            }

            public bool IsFlagged()
            {
                if (NextReference == null)
                    return false;
                return NextReference.State.IsFlagged;
            }

            public LinkedListNode ConvertToMarkedDeleted()
            {
                NextReference.State = new State(NextReference.State.Next, true, NextReference.State.IsFlagged);
                return this;
            }
        }

        private class SearchedNodes
        {
            public LinkedListNode LeftNode { get; }
            public LinkedListNode RightNode { get; }

            public SearchedNodes(LinkedListNode leftNode, LinkedListNode rightNode)
            {
                LeftNode = leftNode;
                RightNode = rightNode;
            }
        }

        public readonly LinkedListNode Head;
        public readonly LinkedListNode Tail;

        public MadLinkedList()
        {
            Tail = new LinkedListNode(default(TKey), default(TValue), null);
            Head = new LinkedListNode(default(TKey), default(TValue), new Reference(new State(Tail, false, false)));
        }

        public bool TryFind(TKey key, ref TValue value)
        {
            var searchedNodes = Search(key, Head);

            if (searchedNodes.RightNode == Tail || searchedNodes.RightNode.Key.CompareTo(key) != 0)
                return false;
            value = searchedNodes.RightNode.Value;
            return true;
        }

        public bool TryInsert(TKey key, TValue value)
        {
            var newNode = new LinkedListNode(key, value, new Reference(new State(null, false, false)));
            var start = Head;

            while (true)
            {
                var searchedNodes = Search(key, start);
                if (searchedNodes.RightNode != Tail && searchedNodes.RightNode.Key.CompareTo(key) == 0)
                    return false;
                newNode.NextReference.State.Next = searchedNodes.RightNode;
                var oldState = searchedNodes.LeftNode.NextReference.State;
                if (oldState.IsMarked || oldState.IsFlagged || oldState.Next != searchedNodes.RightNode)
                {
                    start = GetStart(searchedNodes.LeftNode);
                    continue;
                }
                if (Interlocked.CompareExchange(ref searchedNodes.LeftNode.NextReference.State, new State(newNode, false, false), oldState) == oldState)
                    return true;
            }
        }

        public bool TryDelete(TKey key)
        {
            var start = Head;
            while (true)
            {
                var searchedNodes = Search(key, start);
                if (searchedNodes.RightNode == Tail || searchedNodes.RightNode.Key.CompareTo(key) != 0)
                    return false;

                //TryFlag
                var prevNodeState = searchedNodes.LeftNode.NextReference.State;
                if (prevNodeState.IsMarked || (!prevNodeState.IsFlagged && Interlocked.CompareExchange(ref searchedNodes.LeftNode.NextReference.State, new State(prevNodeState.Next, false, true), prevNodeState) != prevNodeState))
                {
                    start = GetStart(searchedNodes.LeftNode);
                    continue;
                }

                searchedNodes.RightNode.NextReference.State.Backlink = searchedNodes.LeftNode;

                var oldState = searchedNodes.RightNode.NextReference.State;
                if (oldState.IsFlagged)
                {
                    TryPhysicallyDelete(searchedNodes.RightNode, searchedNodes.RightNode.NextReference.State.Next);
                    start = GetStart(searchedNodes.LeftNode);
                    continue;
                }
                var newState = new State(searchedNodes.RightNode.NextReference.State.Next, true, false);
                if (!searchedNodes.RightNode.IsMarked())
                {
                    if (Interlocked.CompareExchange(ref searchedNodes.RightNode.NextReference.State, newState, oldState) == oldState)
                        break;
                }
            }
            return true;
        }

        private LinkedListNode GetStart(LinkedListNode currentNode)
        {
            var start = currentNode;
            while (start != Head || start.IsMarked())
            {
                var backlink = start.NextReference.State.Backlink;
                if (backlink == null)
                    return Head;
                start = backlink;
            }
            return start;
        }

        private bool TryPhysicallyDelete(LinkedListNode currentNode, LinkedListNode nextNode)
        {
            var newState = new State(nextNode.NextReference.State.Next, false, false);
            var oldState = currentNode.NextReference.State;

            if (Interlocked.CompareExchange(ref currentNode.NextReference.State, newState, oldState) != oldState)
            {
                return false;
            }
            return true;
        }

        private SearchedNodes Search(TKey key, LinkedListNode start)
        {
            while (true)
            {
                var currentNode = Head;
                var nextNode = Head.NextReference.State.Next;

                while (nextNode.IsMarked() || nextNode.Key.CompareTo(key) < 0)
                {
                    if (nextNode.IsMarked())
                    {
                        if (!TryPhysicallyDelete(currentNode, nextNode))
                        {
                            nextNode = currentNode.NextReference.State.Next;
                            continue;
                        }
                    }

                    if (nextNode == Tail)
                        break;
                    currentNode = nextNode;
                    nextNode = currentNode.NextReference.State.Next;
                }

                return new SearchedNodes(currentNode, nextNode);
            }
        }
    }
}