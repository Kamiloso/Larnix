using System;
using Larnix.Server.Running;
using RunSuggestions = Larnix.Server.Running.ServerRunner.RunSuggestions;

namespace Larnix.Headless;

internal static class Program
{
    static string WorldDir => Path.Combine(".", "World");

    static void Main(string[] args)
    {
        using ServerRunner runner = new();
        runner.Start(ServerType.Remote, WorldDir, new RunSuggestions());

        while (runner.IsRunning)
        {
            Thread.Sleep(10);
        }
    }
}
