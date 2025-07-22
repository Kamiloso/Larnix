using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace Larnix.Server
{
    public static class DedicatedConsole
    {
        public static void Log(string msg, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        public static void LogSuccess(string msg)
        {
            Log("Success: " + msg, ConsoleColor.Green);
        }

        public static void LogWarning(string msg)
        {
            Log("Warning: " + msg, ConsoleColor.Yellow);
        }

        public static void LogError(string msg)
        {
            Log("Error: " + msg, ConsoleColor.Red);
        }

        public static void SetTitle(string title)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Console.Title = title;
            else
                Console.Write($"\x1b]0;{title}\x07");
        }
    }
}
