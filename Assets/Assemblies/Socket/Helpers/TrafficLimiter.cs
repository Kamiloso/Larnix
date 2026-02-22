using System;
using System.Collections.Generic;

namespace Larnix.Socket
{
    public class TrafficLimiter<T>
    {
        public readonly ulong MAX_TRAFFIC_LOCAL;
        public readonly ulong MAX_TRAFFIC_GLOBAL;

        private readonly Dictionary<T, ulong> _localTraffic = new();
        private ulong _globalTraffic = 0;

        public TrafficLimiter(ulong maxTrafficLocal, ulong maxTrafficGlobal)
        {
            MAX_TRAFFIC_LOCAL = maxTrafficLocal;
            MAX_TRAFFIC_GLOBAL = maxTrafficGlobal;
        }

        public bool TryIncrease(T key)
        {
            if (_globalTraffic >= MAX_TRAFFIC_GLOBAL)
                return false;

            _localTraffic.TryGetValue(key, out ulong localTraffic);
            if (localTraffic >= MAX_TRAFFIC_LOCAL)
                return false;

            _localTraffic[key] = localTraffic + 1;

            _globalTraffic++;
            return true;
        }

        public void Decrease(T key)
        {
            if (!_localTraffic.TryGetValue(key, out ulong localTraffic))
                throw new InvalidOperationException("Local traffic for this key is already zero.");

            localTraffic--;
            if (localTraffic > 0)
                _localTraffic[key] = localTraffic;
            else
                _localTraffic.Remove(key);

            _globalTraffic--;
        }

        public void Reset()
        {
            _localTraffic.Clear();
            _globalTraffic = 0;
        }
    }
}
