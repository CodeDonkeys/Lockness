using System.Collections.Generic;

namespace CodeDonkeys.Lockness
{
    public interface ISet<TElement> : IEnumerable<TElement>
    {
        bool Add(TElement element);

        bool Contains(TElement element);

        bool Remove(TElement element);
    }
}