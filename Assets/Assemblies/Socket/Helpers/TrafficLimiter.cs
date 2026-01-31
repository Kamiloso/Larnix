using System;
using System.Collections.Generic;

namespace Larnix.Socket
{
    public class TrafficLimiter<T>
    {
        public readonly uint MAX_TRAFFIC_LOCAL;
        public readonly uint MAX_TRAFFIC_GLOBAL;

        private readonly Dictionary<T, uint> _localTraffic = new();
        private uint _globalTraffic = 0;

        public TrafficLimiter(uint maxTrafficLocal, uint maxTrafficGlobal)
        {
            MAX_TRAFFIC_LOCAL = maxTrafficLocal;
            MAX_TRAFFIC_GLOBAL = maxTrafficGlobal;
        }

        public bool TryIncrease(T key)
        {
            if (_globalTraffic >= MAX_TRAFFIC_GLOBAL)
                return false;

            _localTraffic.TryGetValue(key, out uint localTraffic);
            if (localTraffic >= MAX_TRAFFIC_LOCAL)
                return false;

            _localTraffic[key] = localTraffic + 1;

            _globalTraffic++;
            return true;
        }

        public void Reset()
        {
            _localTraffic.Clear();
            _globalTraffic = 0;
        }
    }
}
