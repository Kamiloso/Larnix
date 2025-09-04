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

        public static void InitLogs(Action<string> log, Action<string> logWarning, Action<string> logError)
        {
            Log = log;
            LogWarning = logWarning;
            LogError = logError;
        }
    }
}
