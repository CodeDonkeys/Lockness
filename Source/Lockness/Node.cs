using System;

namespace CodeDonkeys.Lockness
{
    [Flags]
    public enum LinkedListLables
    {
        None = 0,
        Flag = 1,
        Mark = 2
    }

    public static class LinkedListLablesExtensions
    {
        public static bool HasLinkedListLable(this LinkedListLables flags, LinkedListLables flag)
        {
            return (flags & flag) == flag;
        }
    }

    public class Node<TElement> where TElement : IComparable
    {
        public readonly TElement Element;
        public Node<TElement> Backlink;
        public AtomicMarkableReference<Node<TElement>, LinkedListLables> NextReference;

        public Node(TElement element, AtomicMarkableReference<Node<TElement>, LinkedListLables> nextReference)
        {
            Element = element;
            NextReference = nextReference;
        }
    }
}