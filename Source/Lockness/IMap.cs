using System;

namespace CodeDonkeys.Lockness
{
    public interface IMap<TKey, TValue> where TKey: IComparable
    {
        bool Add(TKey key, TValue value);

        bool Contains(TKey key);

        bool Remove(TKey key);
    }
}