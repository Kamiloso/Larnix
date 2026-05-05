#nullable enable

namespace Larnix.Server.Data;

internal interface IServerInfo
{
    ServerType Type { get; }
    string WorldPath { get; }
}

internal class ServerInfo : IServerInfo
{
    public ServerType Type { get; }
    public string WorldPath { get; }

    public ServerInfo(ServerType type, string worldPath)
    {
        Type = type;
        WorldPath = worldPath;
    }
}
