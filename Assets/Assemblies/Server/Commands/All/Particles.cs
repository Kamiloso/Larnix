using System;
using System.Collections.Generic;
using Larnix.Model.Enums;
using Larnix.Model.Utils;
using Larnix.Core.Vectors;
using Larnix.Server.Packets;
using Larnix.Socket.Packets;
using Larnix.Core;
using Larnix.Server.Entities;
using System.Linq;
using Larnix.Model;

namespace Larnix.Server.Commands.All;

internal class Particles : BaseCmd
{
    public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Admin;
    public override string Pattern => $"{Name} <type> <x> <y> [uid]";
    public override string ShortDescription => "Spawns particles, optionally connected to entity.";

    private IServer Server => GlobRef.Get<IServer>();
    private IConnectedPlayers ConnectedPlayers => GlobRef.Get<IConnectedPlayers>();

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

        List<string> nearbyPlayers = ConnectedPlayers.AllPlayers
            .Where(nickname => Vec2.Distance(ConnectedPlayers[nickname].RenderPosition, _position) <= Common.ViewDistance)
            .ToList();

        foreach (string nickname in nearbyPlayers)
        {
            Server.Send(nickname, packet);
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
