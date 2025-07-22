using System;
using System.IO;
using UnityEngine;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Larnix.Files;

namespace Larnix
{
    public static class CrashHandler
    {
        private static bool initialized = false;
        private static readonly object logFileLock = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Activate()
        {
            if (initialized) return;
            initialized = true;

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Application.logMessageReceived += OnLogMessageReceived;
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception exception = e.ExceptionObject as Exception;
            string log = "Exception:\n" + exception?.ToString();
            UniversalExceptionHandle(log);
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception)
            {
                string log = "Unity exception:\n" + condition + "\n" + stackTrace;
                UniversalExceptionHandle(log);
            }
        }

        private static void UniversalExceptionHandle(string log)
        {
#if !UNITY_EDITOR
        SaveCrashLog(log);
        Environment.Exit(1); // process kill
#endif
        }

        private static void SaveCrashLog(string message)
        {
            string path = Path.Combine(Application.persistentDataPath, "crash.log");
            try
            {
                lock (logFileLock)
                {
                    File.AppendAllText(path, $"[{DateTime.Now}]\n{message}\n");

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Process.Start(new ProcessStartInfo("notepad.exe", $"\"{path}\"") { UseShellExecute = false });
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Process.Start("xdg-open", $"\"{path}\"");
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        Process.Start("open", $"\"{path}\"");
                    }
                }
            }
            catch
            {
                UnityEngine.Debug.LogError("An error occurred while saving / openning file crash.log. Is it even possible?");
            }
        }
    }
}
