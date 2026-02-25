using System.Collections;
using System.Collections.Concurrent;
using System;
using UnityEngine;
using System.Threading;
using Larnix.Patches;
using Console = Larnix.Core.Console;
using LogType = Larnix.Core.Debug.LogType;

namespace Larnix
{
    public class Debug : MonoBehaviour, IGlobalUnitySingleton
    {
        private readonly struct LogEntry
        {
            public string Message { get; init;}
            public LogType Type { get; init; }

            public LogEntry(string message, LogType type)
            {
                Message = message;
                Type = type;
            }
        }

        private static readonly ConcurrentQueue<LogEntry> _actionQueue = new();
        private static Thread UnityThread;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            UnityThread = Thread.CurrentThread;

            Core.Debug.RedirectLogs(_LogOrEnqueue);
            Core.GamePath.InitPath(Application.persistentDataPath);
        }

        private void Update()
        {
            // Display logs from the main thread
            while (_actionQueue.TryDequeue(out var logEntry))
            {
                _Log(logEntry.Message, logEntry.Type);
            }
        }

        private static void _LogOrEnqueue(string msg, LogType logType)
        {
            if (Thread.CurrentThread == UnityThread)
            {
                _Log(msg, logType);
            }
            else
            {
                _actionQueue.Enqueue(new LogEntry(msg, logType));
            }
        }

        private static void _Log(string msg, LogType logType)
        {
            if (Application.isEditor)
            {
                switch (logType)
                {
                    case LogType.Log: UnityEngine.Debug.Log(msg); break;
                    case LogType.Info: UnityEngine.Debug.Log(msg); break;
                    case LogType.Warning: UnityEngine.Debug.LogWarning(msg); break;
                    case LogType.Error: UnityEngine.Debug.LogError(msg); break;
                    case LogType.Success: UnityEngine.Debug.Log(msg); break;
                    case LogType.Raw: UnityEngine.Debug.Log(msg); break;
                }
            }
            else
            {
                switch (logType)
                {
                    case LogType.Log: Console.Log(msg); break;
                    case LogType.Info: Console.LogInfo(msg); break;
                    case LogType.Warning: Console.LogWarning(msg); break;
                    case LogType.Error: Console.LogError(msg); break;
                    case LogType.Success: Console.LogSuccess(msg); break;
                    case LogType.Raw: Console.LogRaw(msg); break;
                }
            }
        }
    }
}
