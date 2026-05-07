#nullable enable
using System.Net;

namespace Larnix.Socket.Server.Interfaces;

public interface IBanProvider
{
    bool IsBannedNickname(string nickname);
    bool IsBannedIp(IPAddress address);
}
