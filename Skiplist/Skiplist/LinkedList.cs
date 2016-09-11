using System;
using System.Threading;

namespace Skiplist
{
    public class LinkedList<TKey, TValue> where TKey : IComparable
    {
        public class State
        {
            public LinkedListNode Next;
            public bool IsMarked;

            public State(LinkedListNode next, bool isMarked)
            {
                Next = next;
                IsMarked = isMarked;
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

            public bool IsMarkedDeleted()
            {
                if (NextReference == null)
                    return false;
                return NextReference.State.IsMarked;
            }

            public LinkedListNode ConvertToMarkedDeleted()
            {
                NextReference.State = new State(NextReference.State.Next, true);
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

        public LinkedList()
        {
            Tail = new LinkedListNode(default(TKey), default(TValue), null);
            Head = new LinkedListNode(default(TKey), default(TValue), new Reference(new State(Tail, false)));
        }

        public bool TryFind(TKey key, ref TValue value)
        {
            var searchedNodes = Search(key);

            if (searchedNodes.RightNode == Tail || searchedNodes.RightNode.Key.CompareTo(key) != 0)
                return false;
            value = searchedNodes.RightNode.Value;
            return true;
        }

        public bool TryInsert(TKey key, TValue value)
        {
            var newNode = new LinkedListNode(key, value, new Reference(new State(null, false)));

            while (true)
            {
                var searchedNodes = Search(key);
                if (searchedNodes.RightNode != Tail && searchedNodes.RightNode.Key.CompareTo(key) == 0)
                    return false;
                newNode.NextReference.State.Next = searchedNodes.RightNode;
                if (Interlocked.CompareExchange(ref searchedNodes.LeftNode.NextReference.State.Next, newNode, searchedNodes.RightNode) == searchedNodes.RightNode)
                    return true;
            }
        }

        public bool TryDelete(TKey key)
        {
            while (true)
            {
                var searchedNodes = Search(key);
                if (searchedNodes.RightNode == Tail || searchedNodes.RightNode.Key.CompareTo(key) != 0)
                    return false;
                var oldState = searchedNodes.RightNode.NextReference.State;
                var newState = new State(searchedNodes.RightNode.NextReference.State.Next, true);
                if (!searchedNodes.RightNode.IsMarkedDeleted())
                {
                    if (Interlocked.CompareExchange(ref searchedNodes.RightNode.NextReference.State, newState, oldState) == oldState)
                        break;
                }
            }
            return true;
        }

        private SearchedNodes Search(TKey key)
        {
            while (true)
            {
                var currentNode = Head;
                var nextNode = Head.NextReference.State.Next;

                while (nextNode.IsMarkedDeleted() || nextNode.Key.CompareTo(key) < 0)
                {
                    if (nextNode.IsMarkedDeleted())
                    {
                        var newState = new State(nextNode.NextReference.State.Next, false);
                        var oldState = currentNode.NextReference.State;

                        if (Interlocked.CompareExchange(ref currentNode.NextReference.State, newState, oldState) != oldState)
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