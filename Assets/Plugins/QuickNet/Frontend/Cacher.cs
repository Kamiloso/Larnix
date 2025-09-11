using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QuickNet.Channel.Cmds;

namespace QuickNet.Frontend
{
    internal static class Cacher
    {
        private static readonly Dictionary<(string address, string authcode, string nickname), (A_ServerInfo info, long time)> infoDict = new();
        private static readonly object locker = new();

        internal static void AddInfo(string address, string authcode, string nickname, A_ServerInfo info)
        {
            long time = Timestamp.GetTimestamp();
            lock (locker)
            {
                CleanOld();
                infoDict[(address, authcode, nickname)] = (info, time);
            }
        }

        internal static bool TryGetInfo(string address, string authcode, string nickname, out A_ServerInfo info)
        {
            lock (locker)
            {
                CleanOld();

                if (infoDict.TryGetValue((address, authcode, nickname), out var tuple))
                {
                    info = tuple.info;
                    return true;
                }

                info = null;
                return false;
            }
        }

        internal static void IncrementChallengeIDs(string address, string authcode, string nickname, long delta = 1)
        {
            lock (locker)
            {
                var list = infoDict
                    .Where(vkp => vkp.Key == (address, authcode, nickname))
                    .Select(vkp => vkp.Value.info)
                    .ToList();

                foreach (var info in list)
                {
                    info.IncrementChallengeID_ThreadSafe(delta);
                }
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
