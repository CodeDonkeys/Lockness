using System;

namespace CodeDonkeys.Lockness
{
    [Flags]
    public enum SkipListLables
    {
        None = 0,
        Flag = 1,
        Mark = 2
    }

    //Я бы классы с префиксом Concurrent спрятал в ConcurrentSkipList
    public class ConcurrentSkipListNode<TKey, TValue> where TKey : IComparable
    {
        //Не хватает volatile для DownNode, RootNode, Backlink
        public ConcurrentSkipListNode<TKey, TValue> DownNode;
        public ConcurrentSkipListRootNode<TKey, TValue> RootNode;
        public AtomicMarkableReference<ConcurrentSkipListNode<TKey, TValue>, SkipListLables> NextReference;
        public ConcurrentSkipListNode<TKey, TValue> Backlink;

        public ConcurrentSkipListNode(ConcurrentSkipListNode<TKey, TValue> downNode, ConcurrentSkipListRootNode<TKey, TValue> rootNode, AtomicMarkableReference<ConcurrentSkipListNode<TKey, TValue>, SkipListLables> nextReference)
        {
            DownNode = downNode;
            RootNode = rootNode;
            NextReference = nextReference;
        }
    }

    public class ConcurrentSkipListRootNode<TKey, TValue> : ConcurrentSkipListNode<TKey, TValue> where TKey : IComparable
    {
        //Чего-то я не очень понял из чего этот узел все-таки состоит :)

        public readonly TKey Key;
        public readonly TValue Value;
        private TKey key;
        private object p1;
        private object p2;
        private TValue value;

        public ConcurrentSkipListRootNode(TKey key, TValue value, AtomicMarkableReference<ConcurrentSkipListNode<TKey, TValue>, SkipListLables> nextReference) : base(null, null, nextReference)
        {
            Key = key;
            Value = value;
            RootNode = this;
        }
    }

    public class ConcurrentSkipListHeadNode<TKey, TValue> : ConcurrentSkipListNode<TKey, TValue> where TKey : IComparable
    {
        //Не хватает volatile для UpNode
        //Я бы сделал обертку AtomicReference для ссылок
        public ConcurrentSkipListHeadNode<TKey, TValue> UpNode;

        public ConcurrentSkipListHeadNode(ConcurrentSkipListHeadNode<TKey, TValue> downNode, AtomicMarkableReference<ConcurrentSkipListNode<TKey, TValue>, SkipListLables> nextReference, ConcurrentSkipListHeadNode<TKey, TValue> upNode)
            : base(downNode, null, nextReference)
        {
            UpNode = upNode;
        }
    }

    public class ConcurrentSkipListTailNode<TKey, TValue> : ConcurrentSkipListNode<TKey, TValue> where TKey : IComparable
    {
        public ConcurrentSkipListTailNode() : base(null, null, null)
        { }
    }
}