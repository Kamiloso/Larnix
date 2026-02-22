using System;
using System.Collections.Generic;
using Larnix.Core;
using Larnix.Core.Utils;
using Larnix.Core.Vectors;
using Larnix.Packets;
using Larnix.Server.Entities;
using Larnix.Socket.Backend;
using Larnix.Socket.Packets;
using CmdResult = Larnix.Core.ICmdExecutor.CmdResult;

namespace Larnix.Server.Commands.All
{
    internal class Particles : BaseCmd
    {
        public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Admin;
        public override string Pattern => $"{Name} <type> <x> <y> [uid]";
        public override string ShortDescription => "Spawns particles, optionally connected to entity.";

        private QuickServer QuickServer => Ref.QuickServer;
        private PlayerManager PlayerManager => Ref.PlayerManager;

        private ParticleID _particleID;
        private Vec2 _position;
        private ulong _uid;

        public override void Inject(string command)
        {
            if (TrySplit(command, 5, out string[] parts) ||
                TrySplit(command, 4, out parts))
            {
                bool hasUid = parts.Length == 5;

                if (!Enum.TryParse(parts[1], ignoreCase: true, out ParticleID particleID) ||
                    !Enum.IsDefined(typeof(ParticleID), particleID))
                {
                    throw FormatException("Invalid particle type.");
                }

                if (!DoubleUtils.TryParse(parts[2], out double x) ||
                    !DoubleUtils.TryParse(parts[3], out double y))
                {
                    throw FormatException("Cannot parse coordinates.");
                }

                ulong uid = 0;
                if (hasUid)
                {
                    if (!Parsing.TryParseUid(parts[4], out uid))
                    {
                        throw FormatException("Cannot parse uid.");
                    }
                }

                _particleID = particleID;
                _position = new Vec2(x, y);
                _uid = uid;
            }
            else
            {
                throw FormatException(InvalidCmdFormat);
            }
        }

        public override (CmdResult, string) Execute(string sender, PrivilegeLevel privilege)
        {
            Payload packet = new SpawnParticles(_position, _particleID, _uid);

            IEnumerable<string> nearbyPlayers = PlayerManager
                .AllPlayersInRange(_position, Common.PARTICLE_VIEW_DISTANCE);
            
            foreach (string nickname in nearbyPlayers)
            {
                QuickServer.Send(nickname, packet);
            }

            if (_uid == 0)
            {
                return (CmdResult.Success,
                    $"Spawned particles of type '{_particleID}' at {_position}.");
            }
            else
            {
                return (CmdResult.Success,
                    $"Spawned particles of type '{_particleID}' connected to entity {_uid}.");
            }
        }
    }
}
