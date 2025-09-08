using System;
using System.Collections;
using System.Collections.Generic;

namespace Larnix.Core
{
    public static class Debug
    {
        public static Action<string> Log = msg => { };
        public static Action<string> LogWarning = msg => { };
        public static Action<string> LogError = msg => { };
        public static Action<string> LogSuccess = msg => { };
        public static Action<string> LogRaw = msg => { };

        public static void InitLogs(Action<string> log, Action<string> logWarning, Action<string> logError, Action<string> logSuccess, Action<string> logRaw)
        {
            Log = log ?? throw new ArgumentNullException(nameof(log));
            LogWarning = logWarning ?? throw new ArgumentNullException(nameof(logWarning));
            LogError = logError ?? throw new ArgumentNullException(nameof(logError));
            LogSuccess = logSuccess ?? throw new ArgumentNullException(nameof(logSuccess));
            LogRaw = logRaw ?? throw new ArgumentNullException(nameof(logRaw));
        }
    }
}
