using System;
using System.Threading;

namespace CodeDonkeys.Lockness
{
    public class RandomGenerator
    {
        private volatile int state;

        public RandomGenerator() : this(42)
        {
        }

        public RandomGenerator(int seed)
        {
            state = seed;
        }

        public int GetLevelsCount(int maxValue)
        {
            int newState;
            int oldState;
            do
            {
                oldState = state;
                newState = oldState;
                newState ^= newState << 13;
                newState ^= newState >> 17;
                newState ^= newState << 5;
            } while (Interlocked.CompareExchange(ref state, newState, oldState) != oldState);
            return Math.Abs(newState % (maxValue - 1)) + 1;
        }
    }
}