using System;
using Larnix.Server;
using RunSuggestions = Larnix.Server.ServerRunner.RunSuggestions;

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
