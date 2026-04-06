#nullable enable
using Larnix.Server.Packets;
using Larnix.Model.Utils;
using Larnix.Core.Vectors;
using Larnix.Socket.Backend;
using Larnix.Socket.Packets;
using Larnix.Core;
using Larnix.Server.Entities;
using Larnix.Server.Entities.Controllers;
using Larnix.Model.Interfaces;

namespace Larnix.Server.Commands.All;

internal class Tp : BaseCmd
{
    public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Admin;
    public override string Pattern => $"{Name} <nickname> <x> <y>";
    public override string ShortDescription => "Teleports a player to a specific position.";

    private IServer Server => GlobRef.Get<IServer>();
    private IEntityControllers EntityControllers => GlobRef.Get<IEntityControllers>();
    private IConnectedPlayers ConnectedPlayers => GlobRef.Get<IConnectedPlayers>();

    private string _nickname = "";
    private Vec2 _position;

    public override void Inject(string command)
    {
        if (TrySplit(command, 4, out string[] parts))
        {
            _nickname = parts[1];

            if (!Validation.IsGoodNickname(_nickname))
            {
                throw FormatException(Validation.WrongNicknameInfo);
            }

            if (!DoubleUtils.TryParse(parts[2], out double x) ||
                !DoubleUtils.TryParse(parts[3], out double y))
            {
                throw FormatException("Cannot parse coordinates.");
            }

            _position = new Vec2(x, y);
        }
        else
        {
            throw FormatException(InvalidCmdFormat);
        }
    }

    public override (CmdResult, string) Execute(string sender, PrivilegeLevel privilege)
    {
        if (ConnectedPlayers.IsAlive(_nickname))
        {
            Vec2 _realPosition = _position + Common.UpEpsilon;

            Payload packet = new Teleport(_realPosition);
            Server.Send(_nickname, packet);

            ulong uid = ConnectedPlayers.UidByNickname(_nickname);
            var controller = (EntityControllers.GetController(uid) as PlayerController)!;
            controller.AcceptTeleport(_realPosition);

            return (CmdResult.Success, // should show _position
                $"Player {_nickname} has been teleported to {_position}.");
        }
        else
        {
            return (CmdResult.Error,
                $"Player {_nickname} is not alive.");
        }
    }
}
