using System;

namespace Larnix.Core
{
    public static class Debug
    {
        public enum LogType { Log, Info, Warning, Error, Success, Raw }

        private static volatile Action<string, LogType> _Log = (msg, type) =>
        {
            switch (type)
            {
                case LogType.Log: Console.Log(msg); break;
                case LogType.Info: Console.LogInfo(msg); break;
                case LogType.Warning: Console.LogWarning(msg); break;
                case LogType.Error: Console.LogError(msg); break;
                case LogType.Success: Console.LogSuccess(msg); break;
                case LogType.Raw: Console.LogRaw(msg); break;
            }
        };

        public static void Log(object message) => _Log(message?.ToString() ?? "null", LogType.Log);
        public static void LogInfo(object message) => _Log(message?.ToString() ?? "null", LogType.Info);
        public static void LogWarning(object message) => _Log(message?.ToString() ?? "null", LogType.Warning);
        public static void LogError(object message) => _Log(message?.ToString() ?? "null", LogType.Error);
        public static void LogSuccess(object message) => _Log(message?.ToString() ?? "null", LogType.Success);
        public static void LogRaw(object message) => _Log(message?.ToString() ?? "null", LogType.Raw);

        public static void RedirectLogs(Action<string, LogType> printLog)
        {
            _Log = printLog ?? throw new ArgumentNullException(nameof(printLog));
        }
    }
}
