#nullable enable
using Larnix.Core;
using Larnix.Socket.Server.Interfaces;
using System.Linq;

namespace Larnix.Server.Data;

internal class BanProvider : IBanProvider
{
    private ServerConfig Config => GlobRef.Get<ServerConfig>();

    public bool IsBannedNickname(string nickname)
    {
        return Config.Administration_Banned.Contains(nickname);
    }

    public bool IsBannedCidr(string cidr)
    {
        return Config.Administration_Banned.Any(entry =>
        {
            
        });
    }
}
