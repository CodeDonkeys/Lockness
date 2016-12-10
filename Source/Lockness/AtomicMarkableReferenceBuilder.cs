namespace CodeDonkeys.Lockness
{
    internal class AtomicMarkableReferenceBuilder<TReference, TMark>
    {
        public AtomicMarkableReference<TReference, TMark> Build(TReference reference, TMark mark)
        {
            return new AtomicMarkableReference<TReference, TMark>(reference, mark);
        }

        public AtomicMarkableReference<TReference, TMark> Empty()
        {
            return new AtomicMarkableReference<TReference, TMark>(default(TReference), default(TMark));
        }
    }
}