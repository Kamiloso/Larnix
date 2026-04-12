#nullable enable
using Larnix.Core.Serialization;

namespace Larnix.Socket.Helpers.Records;

public record FullLoginData(
    string Address,
    string Authcode,
    FixedString32 Nickname,
    FixedString64 Password
    )
{
    public ServerIdentity ToServerIdentity() => new(Address, Authcode);
    public ServerLogin ToServerLogin() => new(Nickname, Password);
    public ServerDiscovery ToServerDiscovery() => new(Address, Authcode, Nickname);
}
