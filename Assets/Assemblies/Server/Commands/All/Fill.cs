using System;
using Larnix.Blocks;
using Larnix.Blocks.Structs;
using Larnix.Core.Json;
using Larnix.Core.Vectors;
using Larnix.Core;
using CmdResult = Larnix.Core.ICmdExecutor.CmdResult;

namespace Larnix.Server.Commands.All
{
    internal class Fill : BaseCmd
    {
        public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Admin;
        public override string Pattern => $"{Name} <front|back> <type|type:variant> <x1> <y1> <x2> <y2> [json]";
        public override string ShortDescription => "Fills a rectangular area with a block.";

        private IWorldAPI WorldAPI => GlobRef.Get<IWorldAPI>();

        private bool _front;
        private BlockID _blockID;
        private byte _variant;
        private Vec2Int _POS1;
        private Vec2Int _POS2;
        private string _json;

        public override void Inject(string command)
        {
            if (TrySplit(command, 8, out string[] parts, lastJoin: true) ||
                TrySplit(command, 7, out parts))
            {
                bool hasJson = parts.Length == 8;

                if (!Parsing.TryParseFront(parts[1], out bool front))
                {
                    throw FormatException("Invalid front/back layer.");
                }

                if (!Parsing.TryParseBlock(parts[2], out BlockID blockID, out byte variant))
                {
                    throw FormatException("Invalid block type.");
                }

                if (!int.TryParse(parts[3], out int x1) ||
                    !int.TryParse(parts[4], out int y1) ||
                    !int.TryParse(parts[5], out int x2) ||
                    !int.TryParse(parts[6], out int y2))
                {
                    throw FormatException("Cannot parse coordinates.");
                }

                _front = front;
                _blockID = blockID;
                _variant = variant;
                _POS1 = new Vec2Int(x1, y1);
                _POS2 = new Vec2Int(x2, y2);
                _json = hasJson ? parts[7] : "{}";
            }
            else
            {
                throw FormatException(InvalidCmdFormat);
            }
        }

        public override (CmdResult, string) Execute(string sender, PrivilegeLevel privilege)
        {
            Vec2Int min = Vec2Int.MinCorner(_POS1, _POS2);
            Vec2Int max = Vec2Int.MaxCorner(_POS1, _POS2);

            for (int x = min.x; x <= max.x; x++)
                for (int y = min.y; y <= max.y; y++)
                {
                    Vec2Int POS = new Vec2Int(x, y);
                    Block block = WorldAPI.GetBlock(POS, _front);
                    if (block == null)
                    {
                        return (CmdResult.Error,
                            $"Position {POS} is not loaded.");
                    }
                }

            BlockData1 blockTemplate = new(
                id: _blockID,
                variant: _variant,
                data: Storage.FromString(_json)
                );

            for (int x = min.x; x <= max.x; x++)
                for (int y = min.y; y <= max.y; y++)
                {
                    Vec2Int POS = new Vec2Int(x, y);

                    WorldAPI.ReplaceBlock(POS, _front, blockTemplate,
                        IWorldAPI.BreakMode.Replace);
                }

            return (CmdResult.Success,
                $"Filled {(_front ? "front" : "back")} area from {min} to {max} with '{blockTemplate}'.");
        }
    }
}
