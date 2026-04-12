#nullable enable
using Larnix.Core.Serialization;

namespace Larnix.Socket.Helpers.Records;

public record ServerDiscovery(
    string Address,
    string Authcode,
    FixedString32 Nickname
    )
{
    public ServerIdentity ToServerIdentity() => new(Address, Authcode);
}
