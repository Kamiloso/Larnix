#nullable enable
using Larnix.Core.Serialization;

namespace Larnix.Socket.Client.Records;

public record ServerDiscovery(
    string Address,
    string Authcode,
    FixedString32 Nickname
    )
{
    public ServerIdentity ToServerIdentity() => new(Address, Authcode);
}
