using System;
using System.Collections;
using System.Collections.Generic;

namespace QuickNet
{
    public static class Debug
    {
        internal static Action<string> Log = msg => { };
        internal static Action<string> LogWarning = msg => { };
        internal static Action<string> LogError = msg => { };

        public static void InitLogs(Action<string> log, Action<string> logWarning, Action<string> logError)
        {
            Log = log ?? throw new ArgumentNullException(nameof(log));
            LogWarning = logWarning ?? throw new ArgumentNullException(nameof(logWarning));
            LogError = logError ?? throw new ArgumentNullException(nameof(logError));
        }
    }
}
