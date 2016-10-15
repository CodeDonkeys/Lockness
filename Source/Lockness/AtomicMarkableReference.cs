using System.Threading;

namespace CodeDonkeys.Lockness
{
    internal sealed class AtomicMarkableReference<TReference>
    {
        public AtomicMarkableReference(TReference reference, bool mark)
        {
            state = new State(reference, mark);
        }

        public bool CompareAndSet(TReference expectedReference, TReference newReference, bool expectedMark, bool newMark)
        {
            var localState = state;

            return ReferenceEquals(localState.Reference, expectedReference)
                   && localState.Mark == expectedMark
                   && Interlocked.CompareExchange(ref state, new State(newReference, newMark), localState) == localState;
        }

        public void Set(TReference newReference, bool newMark)
        {
            Interlocked.Exchange(ref state, new State(newReference, newMark));
        }

        public TReference Get(out bool mark)
        {
            var localState = state;
            mark = localState.Mark;
            return localState.Reference;
        }

        private volatile State state;

        private class State
        {
            internal readonly TReference Reference;
            internal readonly bool Mark;

            public State(TReference reference, bool mark)
            {
                Reference = reference;
                Mark = mark;
            }
        }
    }
}