using System;
using Larnix.Packets;
using Larnix.Core.Utils;
using Larnix.Core.Vectors;
using Larnix.Server.Terrain;
using CmdResult = Larnix.Core.ICmdExecutor.CmdResult;
using Larnix.Socket.Backend;
using Larnix.Server.Entities;
using Larnix.Entities.All;
using Larnix.Socket.Packets;

namespace Larnix.Server.Commands.All
{
    internal class Tp : BaseCmd
    {
        public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Admin;
        public override string Pattern => $"{Name} <nickname> <x> <y>";
        public override string ShortDescription => "Teleports a player to a specific position.";

        private QuickServer QuickServer => Ref<QuickServer>();
        private EntityManager EntityManager => Ref<EntityManager>();
        private PlayerManager PlayerManager => Ref<PlayerManager>();

        private string _nickname;
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
            if (PlayerManager.IsAlive(_nickname))
            {
                Vec2 _realPosition = _position + Common.UP_EPSILON;

                Payload packet = new Teleport(_realPosition);
                QuickServer.Send(_nickname, packet);
                
                Player controller = (Player)EntityManager.GetPlayerController(_nickname).Controller;
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
}
