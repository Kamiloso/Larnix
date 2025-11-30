using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Larnix.Socket.Packets;

namespace Larnix.Socket.Frontend
{
    internal static class Cacher
    {
        private static readonly Dictionary<(string authcode, string nickname), (A_ServerInfo info, long time)> infoDict = new();
        private static readonly object locker = new();

        public static void AddInfo(string authcode, string nickname, A_ServerInfo info)
        {
            long time = Timestamp.GetTimestamp();
            lock (locker)
            {
                CleanOld();
                infoDict[(authcode, nickname)] = (info, time);
            }
        }

        public static bool TryGetInfo(string authcode, string nickname, out A_ServerInfo info)
        {
            lock (locker)
            {
                CleanOld();

                if (infoDict.TryGetValue((authcode, nickname), out var tuple))
                {
                    info = tuple.info;
                    return true;
                }

                info = null;
                return false;
            }
        }

        public static void RemoveRecord(string authcode, string nickname)
        {
            lock (locker)
            {
                if (infoDict.ContainsKey((authcode, nickname)))
                    infoDict.Remove((authcode, nickname));
            }
        }

        private static void CleanOld()
        {
            var keysToRemove = infoDict
                .Where(kvp => !Timestamp.InTimestamp(kvp.Value.time))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                infoDict.Remove(key);
            }
        }
    }
}
