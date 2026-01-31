using System;
using System.Collections.Generic;

namespace Larnix.Socket
{
    public class ConcurrentLimiter<T>
    {
        /*
            WARNING:
            This class class is weird. It works very unintuitively and
            is different from TrafficLimiter. Be careful when using it!
        */
        
        public readonly uint MAX_CONCURRENT_LOCAL;
        public readonly uint MAX_CONCURRENT_GLOBAL;

        private readonly Dictionary<T, uint> _localConcurrent = new();
        private uint _globalConcurrent = 0;

        public ConcurrentLimiter(uint maxConcurrentLocal, uint maxConcurrentGlobal)
        {
            MAX_CONCURRENT_LOCAL = maxConcurrentLocal;
            MAX_CONCURRENT_GLOBAL = maxConcurrentGlobal;
        }

        public bool TryIncrease(T key)
        {
            if (_globalConcurrent >= MAX_CONCURRENT_GLOBAL)
                return false;

            _localConcurrent.TryGetValue(key, out uint localConcurrent);
            if (localConcurrent >= MAX_CONCURRENT_LOCAL)
                return false;

            _localConcurrent[key] = localConcurrent + 1;
            
            _globalConcurrent++;
            return true;
        }

        public void DecreaseGlobal()
        {
            if (_globalConcurrent == 0)
                throw new InvalidOperationException("Global concurrent is already zero.");

            _globalConcurrent--;
        }

        public void ResetLocal()
        {
            _localConcurrent.Clear();
        }
    }
}
