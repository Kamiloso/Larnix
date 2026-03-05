using System;

namespace Larnix.Socket.Helpers.Limiters
{
    public class Limiter
    {
        public ulong Max { get; init; }
        public ulong Current { get; private set; }

        public Limiter(ulong max)
        {
            Max = max;
            Current = 0;
        }

        public bool TryAdd()
        {
            if (Current < Max)
            {
                Current++;
                return true;
            }
            return false;
        }

        public void Remove()
        {
            if (Current == 0)
                throw new InvalidOperationException("Cannot decrease limit below zero.");

            Current--;
        }

        public void Reset()
        {
            Current = 0;
        }
    }
}
