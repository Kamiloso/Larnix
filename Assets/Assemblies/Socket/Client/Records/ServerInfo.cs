#nullable enable
using Larnix.Core.Serialization;
using Larnix.Core;
using ServerInfoStruct = Larnix.Socket.Payload.Structs.ServerInfo;

namespace Larnix.Socket.Client.Records;

public record ServerInfo(
    string Address,
    ushort Players,
    ushort MaxPlayers,
    Version GameVersion,
    FixedString256 Motd,
    FixedString32 HostUser
    )
{
    internal static ServerInfo FromStruct(string address, in ServerInfoStruct source)
    {
        return new ServerInfo(
            address,
            source.Players,
            source.MaxPlayers,
            source.GameVersion,
            source.Motd,
            source.HostUser
            );
    }
}
