using QuickNet.Processing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace QuickNet
{
    public static class Timestamp
    {
        private const long Window = 6_000; // miliseconds
        private static Dictionary<string, long> TimestampDifferences = new();
        private static object _locker1 = new();

        private static bool _initialized = false;
        private static long startingTimestamp = 0;
        private static Stopwatch stopwatch = new();
        private static object _locker2 = new();

        public static long GetTimestamp()
        {
            lock (_locker2)
            {
                long timestampNow = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
                if (!_initialized)
                {
                    startingTimestamp = timestampNow;
                    stopwatch.Start();
                    _initialized = true;
                }
                return startingTimestamp + (long)stopwatch.Elapsed.TotalMilliseconds;
            }
        }

        public static bool InTimestamp(long timestamp, long window = Window)
        {
            long localTimestamp = GetTimestamp();
            return timestamp >= localTimestamp - window && timestamp <= localTimestamp;
        }

        internal static void SetServerTimestamp(string address, long timestamp)
        {
            lock (_locker1)
            {
                TimestampDifferences[address] = timestamp - GetTimestamp();
            }
        }

        internal static long GetServerTimestamp(string address)
        {
            lock (_locker1)
            {
                if (TimestampDifferences.TryGetValue(address, out long difference))
                    return difference + GetTimestamp();
            }
            throw new InvalidOperationException($"Cannot get server timestamp since it was never declared.");
        }
    }
}
