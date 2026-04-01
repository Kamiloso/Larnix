using System.Collections.Concurrent;
using UnityEngine;
using System.Threading;
using Larnix.Patches;
using Larnix.Core;
using LogType = Larnix.Core.Echo.LogType;

namespace Larnix
{
    public class Debug : MonoBehaviour, IGlobalUnitySingleton
    {
        private record LogEntry(string Message, LogType Type);
        private static readonly ConcurrentQueue<LogEntry> _actionQueue = new();
        private static Thread UnityThread;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init() // Executes only in client builds (server = pure .NET)
        {
            UnityThread = Thread.CurrentThread;

            Echo.RedirectLogs(_LogOrEnqueue);
            Model.GamePath.InitPath(Application.persistentDataPath);
        }

        private void Update()
        {
            // Display logs from other threads
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
    }
}
