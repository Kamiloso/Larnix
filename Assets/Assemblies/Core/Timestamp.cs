#nullable enable
using System;
using System.Diagnostics;

namespace Larnix.Core;

public static class Timestamp
{
    public const long DEFAULT_WINDOW = 6000;

    private static readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private static readonly long _startUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public static long Now()
    {
        return _startUnixMs + _stopwatch.ElapsedMilliseconds;
    }

    public static bool IsWithin(long timestamp, long windowMs = DEFAULT_WINDOW)
    {
        long now = Now();
        return timestamp >= now - windowMs && timestamp <= now;
    }
}
