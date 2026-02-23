using System;
using Larnix.Core.Json;
using Larnix.Core.Utils;
using Larnix.Core.Vectors;
using Larnix.Entities;
using Larnix.Entities.Structs;
using Larnix.Server.Entities;
using Larnix.Core;
using CmdResult = Larnix.Core.ICmdExecutor.CmdResult;

namespace Larnix.Server.Commands.All
{
    internal class Spawn : BaseCmd
    {
        public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Admin;
        public override string Pattern => $"{Name} <type> <x> <y> [json]";
        public override string ShortDescription => "Spawns a new entity.";

        private EntityManager EntityManager => GlobRef.Get<EntityManager>();

        private EntityID _entityID;
        private Vec2 _position;
        private string _json;

        public override void Inject(string command)
        {
            if (TrySplit(command, 5, out string[] parts, lastJoin: true) ||
                TrySplit(command, 4, out parts))
            {
                bool hasJson = parts.Length == 5;

                if (!Enum.TryParse(parts[1], ignoreCase: true, out EntityID entityID) ||
                    !Enum.IsDefined(typeof(EntityID), entityID))
                {
                    throw FormatException("Invalid entity type.");
                }

                if (entityID == EntityID.Player)
                {
                    throw FormatException($"This entity cannot be spawned.");
                }

                if (!DoubleUtils.TryParse(parts[2], out double x) ||
                    !DoubleUtils.TryParse(parts[3], out double y))
                {
                    throw FormatException("Cannot parse coordinates.");
                }

                _entityID = entityID;
                _position = new Vec2(x, y);
                _json = hasJson ? parts[4] : "{}";
            }
            else
            {
                throw FormatException(InvalidCmdFormat);
            }
        }

        public override (CmdResult, string) Execute(string sender, PrivilegeLevel privilege)
        {
            bool success = EntityManager.SummonEntity(new EntityData(
                id: _entityID,
                position: _position,
                rotation: 0.0f,
                data: Storage.FromString(_json)
            ));

            if (success)
            {
                return (CmdResult.Success,
                    $"Spawned '{_entityID}' at position {_position}.");
            }
            else
            {
                return (CmdResult.Error,
                    $"Position {_position} is not loaded.");
            }
        }
    }
}
