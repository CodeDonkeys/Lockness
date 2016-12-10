using System;

namespace CodeDonkeys.Lockness
{
    [Flags]
    internal enum SkipListLables
    {
        None = 0,
        Flag = 1,
        Mark = 2
    }

    //Я бы классы с префиксом Concurrent спрятал в ConcurrentSkipList
    //Я подумала и решила, что переименую)
    internal class SkipListNodeWithBacklink<TElement> where TElement : IComparable
    {
        public volatile SkipListNodeWithBacklink<TElement> DownNode;
        public volatile SkipListRootNodeWithBacklink<TElement> RootNode;
        public AtomicMarkableReference<SkipListNodeWithBacklink<TElement>, SkipListLables> NextReference;
        public volatile SkipListNodeWithBacklink<TElement> Backlink;

        public SkipListNodeWithBacklink(SkipListNodeWithBacklink<TElement> downNode, SkipListRootNodeWithBacklink<TElement> rootNode, AtomicMarkableReference<SkipListNodeWithBacklink<TElement>, SkipListLables> nextReference)
        {
            DownNode = downNode;
            RootNode = rootNode;
            NextReference = nextReference;
        }

        public bool ElementIsEqualsTo(TElement element)
        {
            return (RootNode?.Element.CompareTo(element) ?? 0) == 0;
        }
    }

    internal class SkipListRootNodeWithBacklink<TElement> : SkipListNodeWithBacklink<TElement> where TElement : IComparable
    {
        //Чего-то я не очень понял из чего этот узел все-таки состоит :)
        //Я чет сама в изумлении, откуда это взялось :)

        public readonly TElement Element;

        public SkipListRootNodeWithBacklink(TElement element, AtomicMarkableReference<SkipListNodeWithBacklink<TElement>, SkipListLables> nextReference) : base(null, null, nextReference)
        {
            Element = element;
            RootNode = this;
        }
    }

    internal class SkipListHeadNodeWithBacklink<TElement> : SkipListNodeWithBacklink<TElement> where TElement : IComparable
    {
        //Я бы сделал обертку AtomicReference для ссылок
        //Тут в будущем должны появиться флаги вроде
        public volatile SkipListHeadNodeWithBacklink<TElement> UpNode;

        public SkipListHeadNodeWithBacklink(SkipListHeadNodeWithBacklink<TElement> downNode, AtomicMarkableReference<SkipListNodeWithBacklink<TElement>, SkipListLables> nextReference, SkipListHeadNodeWithBacklink<TElement> upNode)
            : base(downNode, null, nextReference)
        {
            UpNode = upNode;
        }
    }

    internal class SkipListTailNodeWithBacklink<TElement> : SkipListNodeWithBacklink<TElement> where TElement : IComparable
    {
        public SkipListTailNodeWithBacklink() : base(null, null, null)
        { }
    }
}