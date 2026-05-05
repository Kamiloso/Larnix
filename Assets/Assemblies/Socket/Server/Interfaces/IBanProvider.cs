#nullable enable

namespace Larnix.Socket.Server.Interfaces;

public interface IBanProvider
{
    bool IsBannedNickname(string nickname);
    bool IsBannedCIDR(string cidr);
}
