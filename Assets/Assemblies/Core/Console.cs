using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Larnix.Core
{
    public static class Console
    {
        private static Thread InputThread = null;
        private static Queue<string> CommandBuffer = new();
        private static object _lock = new();

        public static void Log(string msg, ConsoleColor color = ConsoleColor.Gray)
        {
            lock (_lock)
            {
                System.Console.ForegroundColor = color;
                System.Console.WriteLine(GetTimestamp() + " " + msg);
                System.Console.ResetColor();
            }
        }

        public static void LogRaw(string msg, ConsoleColor color = ConsoleColor.Gray)
        {
            lock (_lock)
            {
                System.Console.ForegroundColor = color;
                System.Console.Write(msg);
                System.Console.ResetColor();
            }
        }

        public static void LogWarning(string msg)
        {
            lock (_lock)
            {
                LogRaw(GetTimestamp() + " ");
                LogRaw("WARNING: " + msg + "\n", ConsoleColor.Yellow);
            }
        }

        public static void LogError(string msg)
        {
            lock (_lock)
            {
                LogRaw(GetTimestamp() + " ");
                LogRaw("ERROR: " + msg + "\n", ConsoleColor.Red);
            }
        }

        public static void LogSuccess(string msg)
        {
            lock (_lock)
            {
                LogRaw(GetTimestamp() + " ");
                LogRaw("SUCCESS: " + msg + "\n", ConsoleColor.Green);
            }
        }

        public static void SetTitle(string title)
        {
            lock (_lock)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    System.Console.Title = title;
                else
                    System.Console.Write($"\x1b]0;{title}\x07");
            }
        }

        private static string GetTimestamp()
        {
            return DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss]");
        }

        public static void StartInputThread()
        {
            lock (_lock)
            {
                if (InputThread != null)
                    return;

                InputThread = new Thread(InputLoop);
                InputThread.IsBackground = true;
                InputThread.Start();
            }
        }

        public static string GetCommand() // null --> no command in buffer
        {
            lock (_lock)
            {
                if(CommandBuffer.Count > 0)
                    return CommandBuffer.Dequeue();
            }
            return null;
        }

        public static void PushCommandFromCode(string cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("Argument 'cmd' can't be null!");

            lock (_lock)
            {
                CommandBuffer.Enqueue(cmd);
            }
        }

        private static void InputLoop()
        {
            while (true)
            {
                string input = System.Console.ReadLine();
                if (input == null) break;

                lock (_lock)
                {
                    CommandBuffer.Enqueue(input);
                }
            }
        }

        public static string GetInputSync()
        {
            return System.Console.ReadLine();
        }
    }
}
