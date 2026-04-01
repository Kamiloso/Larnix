using System.Net;
using Larnix.GameCore.Utils;

namespace Larnix.Socket.Backend;

public interface IQuickConfig
{
    public ushort MaxClients { get; }
    public String256 Motd { get; }
    public String32 HostUser { get; }

    public bool IsLoopback { get; init; }
    public string DataPath { get; init; }
    public int MaskIPv4 { get; init; }
    public int MaskIPv6 { get; init; }
    public bool AllowRegistration { get; init; }

    public bool IsBanned(string nickname);
    public bool IsBanned(IPAddress address);
}
