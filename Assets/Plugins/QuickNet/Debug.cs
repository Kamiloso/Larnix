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
            Log = log;
            LogWarning = logWarning;
            LogError = logError;
        }
    }
}
