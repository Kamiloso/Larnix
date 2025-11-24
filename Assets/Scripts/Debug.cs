using System.Collections;
using System.Collections.Concurrent;
using System;
using UnityEngine;
using System.Threading;
using System.Diagnostics;
using System.Text;
using System.IO;
using Console = Larnix.Core.Console;

namespace Larnix
{
    public static class Debug
    {
        private static readonly ConcurrentQueue<Action> actionQueue = new();
        private static object _locker = new();

        private static Thread MainThread = null;

        private enum LogType
        {
            Log, Success, Warning,
            Error, RawConsole
        }

        private static void _Log(string msg, LogType logType)
        {
            if (Application.isEditor)
            {
                switch (logType)
                {
                    case LogType.Log:
                    case LogType.Success:
                    case LogType.RawConsole:
                        UnityEngine.Debug.Log(msg);
                        break;

                    case LogType.Warning: UnityEngine.Debug.LogWarning(msg); break;
                    case LogType.Error: UnityEngine.Debug.LogError(msg); break;
                }
            }
            else
            {
                switch (logType)
                {
                    case LogType.Log: Console.Log(msg); break;
                    case LogType.Success: Console.LogSuccess(msg); break;
                    case LogType.Warning: Console.LogWarning(msg); break;
                    case LogType.Error: Console.LogError(msg); break;
                    case LogType.RawConsole: Console.LogRaw(msg); break;
                }
            }
        }

        private static void _LogOrEnqueue(string msg, LogType logType)
        {
            if (logType == LogType.Warning || logType == LogType.Error)
                msg += "\n" + GetFormattedStackTrace(2);

            if (Thread.CurrentThread == MainThread)
            {
                // From main thread - stack trace official
                _Log(msg, logType);
            }
            else
            {
                // From weird thread - stack trace custom
                actionQueue.Enqueue(() => _Log(msg, logType));
            }
        }

        public static void Log(string msg) =>
            _LogOrEnqueue(msg, LogType.Log);

        public static void LogSuccess(string msg) =>
            _LogOrEnqueue(msg, LogType.Success);

        public static void LogWarning(string msg) =>
            _LogOrEnqueue(msg, LogType.Warning);

        public static void LogError(string msg) =>
            _LogOrEnqueue(msg, LogType.Error);

        public static void LogRawConsole(string msg) =>
            _LogOrEnqueue(msg, LogType.RawConsole);

        public static void FlushLogs()
        {
            lock (_locker)
            {
                while (actionQueue.TryDequeue(out var action))
                {
                    action();
                }
            }
        }

        public static string GetFormattedStackTrace(int skipFrames = 0)
        {
            var trace = new StackTrace(skipFrames + 1, true);
            var frames = trace.GetFrames();
            if (frames == null || frames.Length == 0)
                return "<no stack trace>";

            var sb = new StringBuilder();
            int index = 0;
            foreach (var frame in frames)
            {
                var method = frame.GetMethod();
                if (method == null) continue;

                var declaringType = method.DeclaringType;
                string typeName = declaringType != null ? declaringType.FullName : "<global>";
                string methodName = method.Name;

                var fileName = frame.GetFileName();
                int line = frame.GetFileLineNumber();

                // header
                sb.Append("[").Append(index++).Append("] ")
                  .Append(typeName).Append('.').Append(methodName);

                // file + line
                if (!string.IsNullOrEmpty(fileName))
                {
                    sb.Append(" at ")
                      .Append(Path.GetFileName(fileName))
                      .Append(':').Append(line > 0 ? line : 0)
                      .AppendLine();
                }
                else
                {
                    sb.Append("    at <no file info>").AppendLine();
                }
            }

            return sb.ToString();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            MainThread = Thread.CurrentThread;
            Core.Debug.InitLogs(Log, LogWarning, LogError, LogSuccess, LogRawConsole);
        }
    }
}
