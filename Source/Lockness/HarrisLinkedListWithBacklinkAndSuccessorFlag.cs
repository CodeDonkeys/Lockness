using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace CodeDonkeys.Lockness
{
    public sealed class HarrisLinkedListWithBacklinkAndSuccessorFlag<TElement> : ISet<TElement>
    {
        private readonly IComparer<TElement> comparer;
        private readonly LinkedListNode<TElement> head;
        private readonly LinkedListNode<TElement> tail;

        public HarrisLinkedListWithBacklinkAndSuccessorFlag(IComparer<TElement> elementComparer)
        {
            comparer = elementComparer;
            tail = new LinkedListNode<TElement>(default(TElement), null);
            head = new LinkedListNode<TElement>(default(TElement), new AtomicMarkableReference<LinkedListNode<TElement>, LinkedListLables>(tail, LinkedListLables.None));
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            return new Enumerator(head, tail);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Add(TElement element)
        {
            var start = head;

            while (true)
            {
                var nearbyNodes = Search(element, start, SearchStopCondition.Less);
                if (nearbyNodes.RightNode != tail && comparer.Compare(nearbyNodes.RightNode.Element, element) == 0)
                    return false;
                var newNode = new LinkedListNode<TElement>(element, new AtomicMarkableReference<LinkedListNode<TElement>, LinkedListLables>(nearbyNodes.RightNode, LinkedListLables.None));

                LinkedListLables oldLables;
                var oldReference = nearbyNodes.LeftNode.NextReference.Get(out oldLables);
                if (oldLables != LinkedListLables.None || oldReference != nearbyNodes.RightNode)
                {
                    start = GetStart(nearbyNodes.LeftNode);
                    continue;
                }
                if (nearbyNodes.LeftNode.NextReference.CompareAndSet(oldReference, newNode, oldLables, LinkedListLables.None))
                    return true;
            }
        }

        public bool Contains(TElement element)
        {
            var nearbyNodes = Search(element, head, SearchStopCondition.Less);

            if (nearbyNodes.RightNode == tail || comparer.Compare(nearbyNodes.RightNode.Element, element) != 0)
                return false;
            return true;
        }

        public bool Remove(TElement element)
        {
            var nearbyNodes = Search(element, head, SearchStopCondition.Less);
            var deletedNode = nearbyNodes.RightNode;
            if (deletedNode == head || deletedNode == tail || comparer.Compare(deletedNode.Element, element) != 0)
                return false;
            var previousNode = nearbyNodes.LeftNode;
            if (!TrySetFlagLable(ref previousNode, deletedNode))
                return false;
            SetBacklinkAndMarkLable(previousNode, deletedNode);
            TryPhysicallyDeleteNode(previousNode, deletedNode);
            return true;
        }
        private bool TrySetFlagLable(ref LinkedListNode<TElement> previousNode, LinkedListNode<TElement> deletedNode)
        {
            var spin = new SpinWait();
            while (true)
            {
                LinkedListLables oldLable;
                var nextNode = previousNode.NextReference.Get(out oldLable);
                if (oldLable.HasLinkedListLable(LinkedListLables.Flag))
                    return true;
                if (previousNode.NextReference.CompareAndSet(nextNode, nextNode, LinkedListLables.None, LinkedListLables.Flag))
                    return true;

                LinkedListLables currentLable;
                previousNode.NextReference.Get(out currentLable);
                while (currentLable.HasLinkedListLable(LinkedListLables.Mark))
                {
                    previousNode = previousNode.Backlink;
                    previousNode.NextReference.Get(out currentLable);
                }

                if (Search(deletedNode.Element, previousNode, SearchStopCondition.LessOrEqual).LeftNode != deletedNode)
                    return false;

                spin.SpinOnce();
            }
        }

        private void SetBacklinkAndMarkLable(LinkedListNode<TElement> previousNode, LinkedListNode<TElement> deletedNode)
        {
            deletedNode.Backlink = previousNode;
            var spin = new SpinWait();
            while (true)
            {
                LinkedListLables oldLable;
                var nextNode = deletedNode.NextReference.Get(out oldLable);
                if (oldLable.HasLinkedListLable(LinkedListLables.Mark))
                    return;
                if (deletedNode.NextReference.CompareAndSet(nextNode, nextNode, LinkedListLables.None, LinkedListLables.Mark))
                    return;
                spin.SpinOnce();
            }
        }

        private void TryPhysicallyDeleteNode(LinkedListNode<TElement> previousNode, LinkedListNode<TElement> deletedNode)
        {
            LinkedListLables oldLable;
            var nextNode = deletedNode.NextReference.Get(out oldLable);
            previousNode.NextReference.CompareAndSet(deletedNode, nextNode, LinkedListLables.Flag, LinkedListLables.None);
        }

        private enum SearchStopCondition
        {
            Less,
            LessOrEqual
        }

        private SearchedNodes Search(TElement key, LinkedListNode<TElement> start, SearchStopCondition searchStopCondition)
        {
            var stopCondition = searchStopCondition == SearchStopCondition.Less
                ? new Func<TElement, TElement, bool>((key1, key2) => comparer.Compare(key1, key2) < 0)
                : new Func<TElement, TElement, bool>((key1, key2) => comparer.Compare(key1, key2) <= 0);
            var currentNode = head;
            LinkedListLables mark;
            var nextNode = head.NextReference.Get(out mark);
            
            while (mark.HasLinkedListLable(LinkedListLables.Mark) || stopCondition(nextNode.Element, key))
            {
                if (mark.HasLinkedListLable(LinkedListLables.Mark))
                {
                    if (!TryPhysicallyDelete(currentNode, nextNode))
                    {
                        nextNode = currentNode.NextReference.Get(out mark);
                        continue;
                    }
                }

                if (nextNode == tail)
                    break;
                currentNode = nextNode;
                nextNode = currentNode.NextReference.Get(out mark);
            }

            return new SearchedNodes(currentNode, nextNode);
        }

        private LinkedListNode<TElement> GetStart(LinkedListNode<TElement> currentNode)
        {
            var start = currentNode;
            LinkedListLables lables;
            start.NextReference.Get(out lables);
            while (start != head || lables.HasLinkedListLable(LinkedListLables.Mark))
            {
                var backlink = start.Backlink;
                if (backlink == null)
                    return head;
                start = backlink;
                start.NextReference.Get(out lables);
            }
            return start;
        }

        private bool TryPhysicallyDelete(LinkedListNode<TElement> currentNode, LinkedListNode<TElement> nextNode)
        {
            var oldReference = currentNode.NextReference;
            var newReference = nextNode.NextReference;
            currentNode.NextReference.CompareAndSet(oldReference, newReference, LinkedListLables.Flag, LinkedListLables.None);
            return true;
        }

        private class SearchedNodes
        {
            public LinkedListNode<TElement> LeftNode { get; }
            public LinkedListNode<TElement> RightNode { get; }

            public SearchedNodes(LinkedListNode<TElement> leftNode, LinkedListNode<TElement> rightNode)
            {
                LeftNode = leftNode;
                RightNode = rightNode;
            }
        }

        private struct Enumerator : IEnumerator<TElement>
        {
            public TElement Current => currentNode.Element;
            object IEnumerator.Current => Current;

            private LinkedListNode<TElement> currentNode;
            private readonly LinkedListNode<TElement> head;
            private readonly LinkedListNode<TElement> tail;

            public Enumerator(LinkedListNode<TElement> head, LinkedListNode<TElement> tail)
            {
                this.head = head;
                this.tail = tail;
                this.currentNode = head;
            }

            public void Dispose()
            {}

            public bool MoveNext()
            {
                currentNode = currentNode.NextReference;
                return currentNode != tail;
            }

            public void Reset()
            {
                currentNode = head;
            }
        }
    }
}