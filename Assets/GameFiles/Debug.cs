using System.Collections;
using System.Collections.Concurrent;
using System;
using UnityEngine;
using QuickNet;
using UnityEngine.UI;

namespace Larnix
{
    public static class Debug
    {
        private static readonly ConcurrentQueue<(Action<string> action, string msg)> actionQueue = new();
        private static volatile bool _useConsole = false;
        private static object _locker = new();

        public static void Log(string msg) => actionQueue.Enqueue((str =>
        {
            if (_useConsole) Server.Console.Log(str);
            else UnityEngine.Debug.Log(str);
        }, msg));

        public static void LogSuccess(string msg) => actionQueue.Enqueue((str =>
        {
            if (_useConsole) Server.Console.LogSuccess(str);
            else UnityEngine.Debug.Log(str);
        }, msg));

        public static void LogWarning(string msg) => actionQueue.Enqueue((str =>
        {
            if (_useConsole) Server.Console.LogWarning(str);
            else UnityEngine.Debug.LogWarning(str);
        }, msg));

        public static void LogError(string msg) => actionQueue.Enqueue((str =>
        {
            if (_useConsole) Server.Console.LogError(str);
            else UnityEngine.Debug.LogError(str);
        }, msg));

        public static void LogRawConsole(string msg) => actionQueue.Enqueue((str =>
        {
            if (_useConsole) Server.Console.LogRaw(str);
        }, msg));

        public static void LogNoDate(string msg) => actionQueue.Enqueue((str =>
        {
            if (_useConsole) Server.Console.LogRaw(str + "\n");
            else UnityEngine.Debug.Log(str);
        }, msg));

        public static void FlushLogs(bool useConsole)
        {
            lock (_locker)
            {
                _useConsole = useConsole;
                while (actionQueue.TryDequeue(out var tuple))
                {
                    tuple.action(tuple.msg);
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            QuickNet.Debug.InitLogs(Log, LogWarning, LogError);
        }
    }
}
