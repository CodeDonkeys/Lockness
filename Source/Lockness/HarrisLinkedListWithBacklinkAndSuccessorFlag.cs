using System;
using System.Collections;
using System.Collections.Generic;

namespace CodeDonkeys.Lockness
{
    public sealed class HarrisLinkedListWithBacklinkAndSuccessorFlag<TElement> : ISet<TElement> where TElement : IComparable
    {
        private readonly Node<TElement> head;
        private readonly Node<TElement> tail;

        public HarrisLinkedListWithBacklinkAndSuccessorFlag()
        {
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
                if (searchedNodes.RightNode != tail && searchedNodes.RightNode.Element.CompareTo(element) == 0)
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

            if (searchedNodes.RightNode == tail || searchedNodes.RightNode.Element.CompareTo(element) != 0)
                return false;
            return true;
        }

        public bool Remove(TElement element)
        {
            var start = head;
            while (true)
            {
                var searchedNodes = Search(element, start);
                if (searchedNodes.RightNode == tail || searchedNodes.RightNode.Element.CompareTo(element) != 0)
                    return false;

                //TryFlag
                LinkedListLables expectedLables;
                var expexctedReference = searchedNodes.LeftNode.NextReference.Get(out expectedLables);
                if ((expectedLables & LinkedListLables.Mark) == LinkedListLables.Mark || (expectedLables & LinkedListLables.Flag) != LinkedListLables.Flag && !searchedNodes.LeftNode.NextReference.CompareAndSet(expexctedReference, expexctedReference, expectedLables, LinkedListLables.Flag))
                {
                    start = GetStart(searchedNodes.LeftNode);
                    continue;
                }

                searchedNodes.RightNode.Backlink = searchedNodes.LeftNode;

                LinkedListLables oldLables;
                var oldReference = searchedNodes.RightNode.NextReference.Get(out oldLables);
                if ((oldLables & LinkedListLables.Flag) == LinkedListLables.Flag)
                {
                    TryPhysicallyDelete(searchedNodes.RightNode, oldReference);
                    start = GetStart(searchedNodes.LeftNode);
                    continue;
                }

                LinkedListLables newLables;
                var newReference = searchedNodes.RightNode.NextReference.Get(out newLables);
                if ((newLables & LinkedListLables.Mark) != LinkedListLables.Mark)
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

            //TODO Я бы написал Extentions HasFlag, который бы работал для конкретно этого типа и не использовал Enum.HasFlag
            while ((mark & LinkedListLables.Mark) == LinkedListLables.Mark || nextNode.Element.CompareTo(key) < 0)
            {
                if ((mark & LinkedListLables.Mark) == LinkedListLables.Mark)
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
            while (start != head || (lables & LinkedListLables.Mark) == LinkedListLables.Mark)
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
            LinkedListLables lables;
            var oldReference = currentNode.NextReference.Get(out lables);
            var newReference = nextNode.NextReference.Get(out lables);
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
            {
                //TODO Наверное, Dispose кидать в этом месте не самая хорошая идея
                throw new NotImplementedException();
            }

            public bool MoveNext()
            {
                if (currentNode == tail)
                    return false;
                currentNode = currentNode.NextReference;
                return true;
            }

            public void Reset()
            {
                currentNode = head;
            }
        }
    }
}