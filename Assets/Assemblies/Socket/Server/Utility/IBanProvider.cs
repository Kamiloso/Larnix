#nullable enable

namespace Larnix.Socket.Server.Utility;

public interface IBanProvider
{
    bool IsBannedNickname(string nickname);
    bool IsBannedCIDR(string cidr);
}
