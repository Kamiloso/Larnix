using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Concurrent;

namespace Larnix.Core
{
    public static class Console
    {
        private static Thread _inputThread;
        private static ConcurrentQueue<string> _cmdQueue = new();
        private static readonly object _outputLock = new();
        private static readonly object _inputLock = new();

        public static string Timestamp => DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss]");

        public static void SetTitle(string title)
        {
            lock (_outputLock)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    System.Console.Title = title;
                else
                    System.Console.Write($"\x1b]0;{title}\x07");
            }
        }

#region Output

        public static void Log(string msg, ConsoleColor color = ConsoleColor.Gray)
        {
            lock (_outputLock)
            {
                System.Console.ForegroundColor = color;
                System.Console.WriteLine(Timestamp + " " + msg);
                System.Console.ResetColor();
            }
        }

        public static void LogRaw(string msg, ConsoleColor color = ConsoleColor.Gray)
        {
            lock (_outputLock)
            {
                System.Console.ForegroundColor = color;
                System.Console.Write(msg);
                System.Console.ResetColor();
            }
        }

        public static void LogInfo(string msg)
        {
            lock (_outputLock)
            {
                LogRaw(Timestamp + " ");
                LogRaw("INFO: " + msg + "\n", ConsoleColor.Blue);
            }
        }

        public static void LogWarning(string msg)
        {
            lock (_outputLock)
            {
                LogRaw(Timestamp + " ");
                LogRaw("WARNING: " + msg + "\n", ConsoleColor.Yellow);
            }
        }

        public static void LogError(string msg)
        {
            lock (_outputLock)
            {
                LogRaw(Timestamp + " ");
                LogRaw("ERROR: " + msg + "\n", ConsoleColor.Red);
            }
        }

        public static void LogSuccess(string msg)
        {
            lock (_outputLock)
            {
                LogRaw(Timestamp + " ");
                LogRaw("SUCCESS: " + msg + "\n", ConsoleColor.Green);
            }
        }

        public static void Clear()
        {
            lock (_outputLock)
            {
                System.Console.Clear();
            }
        }

#endregion
#region Input

        public static string GetInputSync()
        {
            while (true)
            {
                if (TryPopInput(out string input))
                    return input;

                Thread.Sleep(10);
            }
        }

        public static bool TryPopInput(out string input)
        {
            EnsureInputThread();

            if (_cmdQueue.TryDequeue(out input))
            {
                return true;
            }

            input = default;
            return false;
        }

        private static void InputLoop()
        {
            while (true)
            {
                string input = System.Console.ReadLine();
                if (input == null)
                {
                    Thread.Sleep(500);
                    continue;
                }

                _cmdQueue.Enqueue(input);
            }
        }

        private static void EnsureInputThread()
        {
            lock (_inputLock)
            {
                bool isAlive = _inputThread?.IsAlive ?? false;
                if (!isAlive)
                {
                    _inputThread = new Thread(InputLoop)
                    {
                        IsBackground = true,
                        Name = "Larnix::ConsoleInputThread"
                    };
                    
                    _inputThread.Start();
                }
            }
        }

#endregion

    }
}
