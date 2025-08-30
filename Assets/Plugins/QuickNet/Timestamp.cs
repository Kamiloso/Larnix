using QuickNet.Processing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace QuickNet
{
    public static class Timestamp
    {
        public const long Window = 6_000; // miliseconds
        private static Dictionary<EndPoint, long> TimestampDifferences = new();
        private static object locker = new();

        public static long GetTimestamp()
        {
            return ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
        }

        public static bool InTimestamp(long timestamp)
        {
            long localTimestamp = GetTimestamp();
            return timestamp >= localTimestamp - Window && timestamp <= localTimestamp;
        }

        internal static void SetServerTimestamp(IPEndPoint endPoint, long timestamp)
        {
            lock (locker)
            {
                TimestampDifferences[endPoint] = timestamp - GetTimestamp();
            }
        }

        internal static long GetServerTimestamp(IPEndPoint endPoint)
        {
            lock (locker)
            {
                if (TimestampDifferences.TryGetValue(endPoint, out long difference))
                    return GetTimestamp() + difference;
            }
            throw new InvalidOperationException($"Cannot get server timestamp of {endPoint} since it was never declared.");
        }
    }
}
