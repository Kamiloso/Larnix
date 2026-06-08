#nullable enable
using Larnix.Core;
using Larnix.Server.Run.Records;
using System.Collections.Generic;
using System.Threading;

namespace Larnix.Server.Run;

public static class ServerRunner
{
    private static readonly ThreadLocal<Dictionary<string, ServerHandle>> _servers = new(() => new());
    private static Dictionary<string, ServerHandle> Servers => _servers.Value;

    public static IServerSpy Start(string key, RunInfo runInfo)
    {
        if (!Servers.TryGetValue(key, out var handle))
        {
            Echo.LogInfo($"Starting server with key '{key}'...");

            Servers[key] = handle = new ServerHandle(runInfo);
        }

        return handle;
    }

    public static void Stop(string key)
    {
        if (Servers.TryGetValue(key, out var handle))
        {
            Echo.LogInfo($"Stopping server with key '{key}'...");

            handle.StopSync();
            handle.Dispose();

            Servers.Remove(key);
        }
    }
}
