using System;
using Larnix.Server;

namespace Larnix.Headless;

internal static class Program
{
    static string WorldDir => Path.Combine(".", "World");

    static void Main(string[] args)
    {
        ServerRunner runner = ServerRunner.Instance;
        runner.Start(ServerType.Remote, worldPath: WorldDir, null);

        while (runner.IsRunning)
        {
            Thread.Sleep(10);
        }

        runner.Stop();
    }
}
