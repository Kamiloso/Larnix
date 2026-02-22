using System;
using Larnix.Blocks;
using Larnix.Blocks.Structs;
using Larnix.Core.Json;
using Larnix.Core.Utils;
using Larnix.Core.Vectors;
using Larnix.Server.Terrain;
using CmdResult = Larnix.Core.ICmdExecutor.CmdResult;

namespace Larnix.Server.Commands.All
{
    internal class Replace : BaseCmd
    {
        public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Admin;
        public override string Pattern => $"{Name} <front|back> <type|type:variant> <x> <y> [json]";
        public override string ShortDescription => "Replaces a block.";

        private IWorldAPI WorldAPI => Ref.IWorldAPI;

        private bool _front;
        private BlockID _blockID;
        private byte _variant;
        private Vec2Int _POS;
        private string _json;

        public override void Inject(string command)
        {
            if (TrySplit(command, 6, out string[] parts, lastJoin: true) ||
                TrySplit(command, 5, out parts))
            {
                bool hasJson = parts.Length == 6;

                if (!Parsing.TryParseFront(parts[1], out bool front))
                {
                    throw FormatException("Invalid front/back layer.");
                }

                if (!Parsing.TryParseBlock(parts[2], out BlockID blockID, out byte variant))
                {
                    throw FormatException("Invalid block type.");
                }

                if (!int.TryParse(parts[3], out int x) ||
                    !int.TryParse(parts[4], out int y))
                {
                    throw FormatException("Cannot parse coordinates.");
                }

                _front = front;
                _blockID = blockID;
                _variant = variant;
                _POS = new Vec2Int(x, y);
                _json = hasJson ? parts[5] : "{}";
            }
            else
            {
                throw FormatException(InvalidCmdFormat);
            }
        }

        public override (CmdResult, string) Execute(string sender, PrivilegeLevel privilege)
        {
            Block block = WorldAPI.GetBlock(_POS, _front);
            if (block != null)
            {
                BlockData1 blockTemplate = new(
                    id: _blockID,
                    variant: _variant,
                    data: Storage.FromString(_json)
                    );

                WorldAPI.ReplaceBlock(_POS, _front, blockTemplate,
                    IWorldAPI.BreakMode.Replace);

                return (CmdResult.Success,
                    $"Replaced {(_front ? "front" : "back")} block at position {_POS} with '{blockTemplate}'.");
            }

            return (CmdResult.Error,
                $"Position {_POS} is not loaded.");
        }
    }
}
