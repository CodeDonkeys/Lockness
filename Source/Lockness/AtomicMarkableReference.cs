using System.Threading;

namespace CodeDonkeys.Lockness
{
    public sealed class AtomicMarkableReference<TReference, TMark>
    {
        public static AtomicMarkableReference<TReference, TMark> Empty()
        {
            return new AtomicMarkableReference<TReference, TMark>(default(TReference), default(TMark));
        }

        public static AtomicMarkableReference<TReference, TMark> New(TReference reference, TMark mark)
        {
            return new AtomicMarkableReference<TReference, TMark>(reference, mark);
        }

        public AtomicMarkableReference(TReference reference, TMark mark)
        {
            state = new State(reference, mark);
        }

        public bool CompareAndSet(TReference expectedReference, TReference newReference, TMark expectedMark, TMark newMark)
        {
            var localState = state;

            return ReferenceEquals(localState.Reference, expectedReference)
                   && localState.Mark.Equals(expectedMark)
                   && Interlocked.CompareExchange(ref state, new State(newReference, newMark), localState) == localState;
        }

        public void Set(TReference newReference, TMark newMark)
        {
            Interlocked.Exchange(ref state, new State(newReference, newMark));
        }

        public TReference Get(out TMark mark)
        {
            var localState = state;
            mark = localState.Mark;
            return localState.Reference;
        }

        public static implicit operator TReference(AtomicMarkableReference<TReference, TMark> instance)
        {
            return instance.state.Reference;
        }

        private volatile State state;

        private class State
        {
            internal readonly TReference Reference;
            internal readonly TMark Mark;

            public State(TReference reference, TMark mark)
            {
                Reference = reference;
                Mark = mark;
            }
        }
    }
}