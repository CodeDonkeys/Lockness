using System;
using System.Collections;
using System.Collections.Generic;

namespace CodeDonkeys.Lockness
{
    public sealed class HarrisLinkedListWithBacklinkAndSuccessorFlag<TElement> : ISet<TElement>
    {
        private readonly IComparer<TElement> comparer;
        private readonly Node<TElement> head;
        private readonly Node<TElement> tail;

        public HarrisLinkedListWithBacklinkAndSuccessorFlag(IComparer<TElement> elementComparer)
        {
            comparer = elementComparer;
            tail = new Node<TElement>(default(TElement), null);
            head = new Node<TElement>(default(TElement), new AtomicMarkableReference<Node<TElement>, LinkedListLables>(tail, LinkedListLables.None));
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
                var searchedNodes = Search(element, start);
                if (searchedNodes.RightNode != tail && comparer.Compare(searchedNodes.RightNode.Element, element) == 0)
                    return false;
                var newNode = new Node<TElement>(element, new AtomicMarkableReference<Node<TElement>, LinkedListLables>(searchedNodes.RightNode, LinkedListLables.None));

                LinkedListLables oldLables;
                var oldReference = searchedNodes.LeftNode.NextReference.Get(out oldLables);
                if (oldLables != LinkedListLables.None || oldReference != searchedNodes.RightNode)
                {
                    start = GetStart(searchedNodes.LeftNode);
                    continue;
                }
                if (searchedNodes.LeftNode.NextReference.CompareAndSet(oldReference, newNode, oldLables, LinkedListLables.None))
                    return true;
            }
        }

        public bool Contains(TElement element)
        {
            var searchedNodes = Search(element, head);

            if (searchedNodes.RightNode == tail || comparer.Compare(searchedNodes.RightNode.Element, element) != 0)
                return false;
            return true;
        }

        public bool Remove(TElement element)
        {
            var start = head;
            while (true)
            {
                var searchedNodes = Search(element, start);
                if (searchedNodes.RightNode == tail || comparer.Compare(searchedNodes.RightNode.Element, element) != 0)
                    return false;

                //TryFlag
                LinkedListLables expectedLables;
                var expexctedReference = searchedNodes.LeftNode.NextReference.Get(out expectedLables);
                if (expectedLables.HasLinkedListLable(LinkedListLables.Mark) || !(expectedLables.HasLinkedListLable(LinkedListLables.Flag)) && !searchedNodes.LeftNode.NextReference.CompareAndSet(expexctedReference, expexctedReference, expectedLables, LinkedListLables.Flag))
                {
                    start = GetStart(searchedNodes.LeftNode);
                    continue;
                }

                searchedNodes.RightNode.Backlink = searchedNodes.LeftNode;

                LinkedListLables oldLables;
                var oldReference = searchedNodes.RightNode.NextReference.Get(out oldLables);
                if (oldLables.HasLinkedListLable(LinkedListLables.Flag))
                {
                    TryPhysicallyDelete(searchedNodes.RightNode, oldReference);
                    start = GetStart(searchedNodes.LeftNode);
                    continue;
                }

                LinkedListLables newLables;
                var newReference = searchedNodes.RightNode.NextReference.Get(out newLables);
                if (!newLables.HasLinkedListLable(LinkedListLables.Mark))
                {
                    if (searchedNodes.RightNode.NextReference.CompareAndSet(oldReference, newReference, oldLables, LinkedListLables.Mark))
                        break;
                }
            }
            return true;
        }

        private SearchedNodes Search(TElement key, Node<TElement> start)
        {
            var currentNode = head;
            LinkedListLables mark;
            var nextNode = head.NextReference.Get(out mark);
            
            while (mark.HasLinkedListLable(LinkedListLables.Mark) || comparer.Compare(nextNode.Element, key) < 0)
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

        private Node<TElement> GetStart(Node<TElement> currentNode)
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

        private bool TryPhysicallyDelete(Node<TElement> currentNode, Node<TElement> nextNode)
        {
            var oldReference = currentNode.NextReference;
            var newReference = nextNode.NextReference;
            currentNode.NextReference.CompareAndSet(oldReference, newReference, LinkedListLables.Flag, LinkedListLables.None);
            return true;
        }

        private class SearchedNodes
        {
            public Node<TElement> LeftNode { get; }
            public Node<TElement> RightNode { get; }

            public SearchedNodes(Node<TElement> leftNode, Node<TElement> rightNode)
            {
                LeftNode = leftNode;
                RightNode = rightNode;
            }
        }

        private struct Enumerator : IEnumerator<TElement>
        {
            public TElement Current => currentNode.Element;
            object IEnumerator.Current => Current;

            private Node<TElement> currentNode;
            private readonly Node<TElement> head;
            private readonly Node<TElement> tail;

            public Enumerator(Node<TElement> head, Node<TElement> tail)
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