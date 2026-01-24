using System;
using System.Collections;
using System.Collections.Generic;

namespace Larnix.Core
{
    public static class Debug
    {
        private static object _lock = new();

        public static Action<string> Log { get; private set; } = msg => Console.Log(msg);
        public static Action<string> LogWarning { get; private set; } = msg => Console.LogWarning(msg);
        public static Action<string> LogError { get; private set; } = msg => Console.LogError(msg);
        public static Action<string> LogSuccess { get; private set; } = msg => Console.LogSuccess(msg);
        public static Action<string> LogRaw { get; private set; } = msg => Console.LogRaw(msg);

        public static void RedirectLogs(
            Action<string> log,
            Action<string> logWarning,
            Action<string> logError,
            Action<string> logSuccess,
            Action<string> logRaw)
        {
            lock (_lock)
            {
                Log = log ?? throw new ArgumentNullException(nameof(log));
                LogWarning = logWarning ?? throw new ArgumentNullException(nameof(logWarning));
                LogError = logError ?? throw new ArgumentNullException(nameof(logError));
                LogSuccess = logSuccess ?? throw new ArgumentNullException(nameof(logSuccess));
                LogRaw = logRaw ?? throw new ArgumentNullException(nameof(logRaw));
            }
        }
    }
}
