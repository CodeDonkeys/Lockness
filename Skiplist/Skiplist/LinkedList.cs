using System;
using System.Threading;

namespace Skiplist
{
    
    public class LinkedList<TKey, TValue> where TKey : IComparable
    {
        public class LinkedListNode<TKey, TValue> where TKey : IComparable
        {
            public readonly TKey Key;
            public TValue Value;
            public LinkedListNode<TKey, TValue> Next;
            private bool isMarkedForDelete;

            public LinkedListNode(TKey key, TValue value, LinkedListNode<TKey, TValue> next)
            {
                Key = key;
                Value = value;
                Next = next;
            }

            private LinkedListNode(LinkedListNode<TKey, TValue> node, bool isMarkedForDelete)
                : this(node.Key, node.Value, node.Next)
            {
                this.isMarkedForDelete = isMarkedForDelete;
            }

            public bool IsMarkedDeleted()
            {
                return isMarkedForDelete;
            }

            public LinkedListNode<TKey, TValue> ConvertToMarkedDeleted()
            {
                return new LinkedListNode<TKey, TValue>(this, true);
            }

            public LinkedListNode<TKey, TValue> ConvertToUnmarkedDeleted()
            {
                return new LinkedListNode<TKey, TValue>(this, true);
            }
        }


        public readonly LinkedListNode<TKey, TValue> Head;
        public readonly LinkedListNode<TKey, TValue> Tail;

        public LinkedList()
        {
            Tail = new LinkedListNode<TKey, TValue>(default(TKey), default(TValue), null);
            Head = new LinkedListNode<TKey, TValue>(default(TKey), default(TValue), Tail);
        }

        private LinkedListNode<TKey, TValue> Search(TKey key, out LinkedListNode<TKey, TValue> leftNode)
        {
            while (true)
            {
                var currentNode = Head;
                var nextNode = Head.Next;
                leftNode = currentNode;
                var leftNodeNext = nextNode;

                do
                {
                    if (!nextNode.IsMarkedDeleted())
                    {
                        leftNode = currentNode;
                        leftNodeNext = nextNode;
                    }
                    currentNode = nextNode.ConvertToUnmarkedDeleted();
                    if (currentNode == Tail)
                        break;
                    nextNode = currentNode.Next;
                } while (nextNode.IsMarkedDeleted() || currentNode.Key.CompareTo(key) < 0);

                var rightNode = currentNode;

                if (leftNodeNext == rightNode)
                {
                    if (rightNode != Tail && rightNode.Next.IsMarkedDeleted())
                        continue;
                    return rightNode;
                }
                
                if (Interlocked.CompareExchange(ref leftNode.Next, rightNode, leftNodeNext) == rightNode)
                {
                    if (rightNode == Tail || !rightNode.Next.IsMarkedDeleted())
                        return rightNode;
                }
            }
        }

        public TValue Find(TKey key)
        {
            LinkedListNode<TKey, TValue> leftNode;
            var rightNode = Search(key, out leftNode);

            if (rightNode == Tail || rightNode.Key.CompareTo(key) != 0)
                return default(TValue);
            return rightNode.Value;
        }

        public bool TryInsert(TKey key, TValue value)
        {
            var newNode = new LinkedListNode<TKey, TValue>(key, value, null);

            while (true)
            {
                LinkedListNode<TKey, TValue> leftNode;
                var rightNode = Search(key, out leftNode);
                if (rightNode != Tail && rightNode.Key.CompareTo(key) == 0)
                    return false;
                newNode.Next = rightNode;
                if (Interlocked.CompareExchange(ref leftNode.Next, newNode, rightNode) == newNode)
                    return true;
            }
        }

        public bool TryDelete(TKey key)
        {
            LinkedListNode<TKey, TValue> rightNode;
            LinkedListNode<TKey, TValue> rightNodeNext;
            LinkedListNode<TKey, TValue> leftNode;

            while (true)
            {
                rightNode = Search(key, out leftNode);
                if (rightNode == Tail || rightNode.Key.CompareTo(key) != 0)
                    return false;
                rightNodeNext = rightNode.Next;
                if (!rightNodeNext.IsMarkedDeleted())
                {
                    var markedDeletedRightNodeNext = rightNodeNext.ConvertToMarkedDeleted();
                    if (Interlocked.CompareExchange(ref rightNode.Next, markedDeletedRightNodeNext, rightNodeNext) == markedDeletedRightNodeNext)
                        break;
                }
            }
            if (Interlocked.CompareExchange(ref leftNode.Next, rightNodeNext, rightNode) != rightNodeNext)
                Search(rightNode.Key, out leftNode);
            return true;
        }
    }
}