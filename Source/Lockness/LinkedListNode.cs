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
        public static bool HasLinkedListLable(this LinkedListLables thisLable, LinkedListLables expectedLable)
        {
            return (thisLable & expectedLable) == expectedLable;
        }
    }

    public class LinkedListNode<TElement>
    {
        public readonly TElement Element;
        public LinkedListNode<TElement> Backlink;
        public AtomicMarkableReference<LinkedListNode<TElement>, LinkedListLables> NextReference;

        public LinkedListNode(TElement element, AtomicMarkableReference<LinkedListNode<TElement>, LinkedListLables> nextReference)
        {
            Element = element;
            NextReference = nextReference;
        }
    }
}