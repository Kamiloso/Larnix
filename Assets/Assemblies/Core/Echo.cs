#nullable enable
using System;

namespace Larnix.Core;

public static class Echo
{
    public enum LogType : byte { Log, Info, Warning, Error, Success, Raw }
    private static string STR(object? obj) => obj?.ToString() ?? "null";

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

    public static void RedirectLogs(Action<string, LogType> printLog)
    {
        _Log = printLog ?? throw new ArgumentNullException(nameof(printLog));
    }

    // Console methods (console only)
    public static void SetTitle(string title) => Console.SetTitle(title);
    public static void Cls() => Console.Clear();
    public static void PrintBorder() => LogRaw($"{new string('-', 60)}\n");

    // Input methods
    public static bool TryPopLine(out string? line) => Console.TryPopInput(out line);
    public static string ReadLineSync() => Console.GetInputSync();

    // Log methods
    public static void Log(object? message, LogType logType) => _Log(STR(message), logType);
    public static void Log(object? message) => _Log(STR(message), LogType.Log);
    public static void LogInfo(object? message) => _Log(STR(message), LogType.Info);
    public static void LogWarning(object? message) => _Log(STR(message), LogType.Warning);
    public static void LogError(object? message) => _Log(STR(message), LogType.Error);
    public static void LogSuccess(object? message) => _Log(STR(message), LogType.Success);
    public static void LogRaw(object? message) => _Log(STR(message), LogType.Raw);
}
