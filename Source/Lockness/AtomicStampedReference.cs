using System.Threading;

namespace CodeDonkeys.Lockness
{
    internal sealed class AtomicStampedReference<TReference>
    {
        public AtomicStampedReference(TReference reference, long stamp)
        {
            state = new State(reference, stamp);
        }

        public bool CompareAndSet(TReference expectedReference, TReference newReference, long expectedStamp, long newStamp)
        {
            var localState = state;

            return ReferenceEquals(localState.Reference, expectedReference)
                   && localState.Stamp == expectedStamp
                   && Interlocked.CompareExchange(ref state, new State(newReference, newStamp), localState) == localState;
        }

        public void Set(TReference newReference, long newStamp)
        {
            Interlocked.Exchange(ref state, new State(newReference, newStamp));
        }

        public TReference Get(out long stamp)
        {
            var localState = state;
            stamp = localState.Stamp;
            return localState.Reference;
        }

        private volatile State state;

        private class State
        {
            internal readonly TReference Reference;
            internal readonly long Stamp;

            public State(TReference reference, long stamp)
            {
                Reference = reference;
                Stamp = stamp;
            }
        }
    }
}