using System;
using System.Collections;
using System.Collections.Generic;

namespace Larnix.Core
{
    public static class Debug
    {
        private static object _lock = new();
        private static bool _initialized;

        public static Action<string> Log { get; private set; } = _ => {};
        public static Action<string> LogWarning { get; private set; } = _ => {};
        public static Action<string> LogError { get; private set; } = _ => {};
        public static Action<string> LogSuccess { get; private set; } = _ => {};
        public static Action<string> LogRaw { get; private set; } = _ => {};

        public static void InitLogs(
            Action<string> log,
            Action<string> logWarning,
            Action<string> logError,
            Action<string> logSuccess,
            Action<string> logRaw)
        {
            lock (_lock)
            {
                if (_initialized)
                    throw new InvalidOperationException("Debug already initialized.");

                Log = log ?? throw new ArgumentNullException(nameof(log));
                LogWarning = logWarning ?? throw new ArgumentNullException(nameof(logWarning));
                LogError = logError ?? throw new ArgumentNullException(nameof(logError));
                LogSuccess = logSuccess ?? throw new ArgumentNullException(nameof(logSuccess));
                LogRaw = logRaw ?? throw new ArgumentNullException(nameof(logRaw));

                _initialized = true;
            }
        }
    }
}
