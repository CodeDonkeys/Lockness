using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace CodeDonkeys.Lockness
{
    public sealed class HarrisLinkedList<TElement> : ISet<TElement>
    {
        public HarrisLinkedList(IComparer<TElement> elementComparer)
        {
            comparer = elementComparer;
            tail = new HarrisLinkedListNode(default(TElement), new AtomicMarkableReference<HarrisLinkedListNode, bool>(null, false));
            head = new HarrisLinkedListNode(default(TElement), new AtomicMarkableReference<HarrisLinkedListNode, bool>(tail, false));
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
            var spinWait = new SpinWait();
            var node = new HarrisLinkedListNode(element, new AtomicMarkableReference<HarrisLinkedListNode, bool>(null, false));

            for (;;)
            {
                spinWait.SpinOnce();

                var window = Search(element, ref spinWait);

                var left = window.Left;
                var right = window.Right;

                if (right != tail && comparer.Compare(element, right.Element) == 0)
                {
                    return false;
                }

                bool leftNextMark;
                var leftNext = left.Next.Get(out leftNextMark);

                if (leftNextMark || leftNext != right)
                {
                    continue;
                }

                node.Next.Set(right, false);

                if (left.Next.CompareAndSet(leftNext, node, false, false))
                {
                    return true;
                }
            }
        }

        public bool Contains(TElement element)
        {
            var spinWait = new SpinWait();
            var window = Search(element, ref spinWait);
            bool rightNextMark;
            window.Right.Next.Get(out rightNextMark);
            return window.Right != tail && comparer.Compare(window.Right.Element, element) == 0 && !rightNextMark;
        }

        public bool Remove(TElement element)
        {
            var spinWait = new SpinWait();

            for (;;)
            {
                spinWait.SpinOnce();

                var window = Search(element, ref spinWait);

                var right = window.Right;

                if (right == tail || comparer.Compare(element, right.Element) != 0)
                {
                    return false;
                }

                bool rightNextMark;
                var rightNext = right.Next.Get(out rightNextMark);
                if (rightNextMark)
                {
                    return false;
                }

                if (right.Next.CompareAndSet(rightNext, rightNext, false, true))
                {
                    return true;
                }
            }
        }

        private struct Enumerator : IEnumerator<TElement>
        {
            private readonly HarrisLinkedListNode head;
            private readonly HarrisLinkedListNode tail;
            private HarrisLinkedListNode current;

            public TElement Current => current.Element;

            object IEnumerator.Current => current.Element;

            internal Enumerator(HarrisLinkedListNode head, HarrisLinkedListNode tail)
            {
                this.head = head;
                this.tail = tail;
                current = head;
            }

            public void Dispose()
            {
            }
            
            public bool MoveNext()
            {
                current = current.Next;
                return current != tail;
            }
            
            void IEnumerator.Reset()
            {
                current = head;
            }
        }

        private Window Search(TElement element, ref SpinWait spinWait)
        {
            for (;;)
            {
                spinWait.SpinOnce();

                HarrisLinkedListNode predeсessor = head;
                HarrisLinkedListNode current = predeсessor.Next;

                for (;;)
                {
                    bool successorMark;
                    HarrisLinkedListNode successor = current.Next.Get(out successorMark);

                    if (successorMark && !predeсessor.Next.CompareAndSet(current, successor, false, false))
                    {
                        break;
                    }

                    if (current == tail || comparer.Compare(element, current.Element) <= 0)
                    {
                        return new Window(predeсessor, current);
                    }

                    predeсessor = current;
                    current = successor;
                }
            }
        }

        private readonly HarrisLinkedListNode head;
        private readonly HarrisLinkedListNode tail;
        private readonly IComparer<TElement> comparer;

        internal sealed class HarrisLinkedListNode
        {
            internal readonly TElement Element;
            internal readonly AtomicMarkableReference<HarrisLinkedListNode, bool> Next;

            internal HarrisLinkedListNode(TElement element, AtomicMarkableReference<HarrisLinkedListNode, bool> next)
            {
                Next = next;
                Element = element;
            }
        }

        private struct Window
        {
            internal readonly HarrisLinkedListNode Left;
            internal readonly HarrisLinkedListNode Right;

            public Window(HarrisLinkedListNode left, HarrisLinkedListNode right)
            {
                Left = left;
                Right = right;
            }
        }
    }
}