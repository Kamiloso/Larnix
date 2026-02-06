using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Socket.Packets;
using Larnix.Blocks.Structs;
using Larnix.Packets;
using Larnix.Core.Vectors;

namespace Larnix.Client.Terrain
{
    public static class WorldAPI
    {
        public static bool CanBePlaced(Vec2Int POS, BlockData1 item, bool front)
        {
            BlockData2 blockData = Ref.GridManager.BlockDataAtPOS(POS);
            bool isLocked = Ref.GridManager.IsBlockLocked(POS);

            if (blockData != null && !isLocked)
            {
                return BlockInteractions.CanBePlaced(blockData, item, front);
            }
            return false;
        }

        public static bool CanBeBroken(Vec2Int POS, BlockData1 tool, bool front)
        {
            BlockData2 blockData = Ref.GridManager.BlockDataAtPOS(POS);
            bool isLocked = Ref.GridManager.IsBlockLocked(POS);

            if (blockData != null && !isLocked)
            {
                BlockData1 item = front ? blockData.Front : blockData.Back;
                return BlockInteractions.CanBeBroken(blockData, item, tool, front);
            }
            return false;
        }

        public static void PlaceBlock(Vec2Int POS, BlockData1 item, bool front)
        {
            long operation = Ref.GridManager.PlaceBlockClient(POS, item, front);
            SendBlockChange(POS, item, new(), front, operation, 0);
        }

        public static void BreakBlock(Vec2Int POS, BlockData1 tool, bool front)
        {
            BlockData2 _oldblock = Ref.GridManager.BlockDataAtPOS(POS);
            BlockData1 oldblock = front ? _oldblock.Front : _oldblock.Back;
            long operation = Ref.GridManager.BreakBlockClient(POS, front);
            SendBlockChange(POS, oldblock, tool, front, operation, 1);
        }

        private static void SendBlockChange(Vec2Int POS, BlockData1 item, BlockData1 tool, bool front, long operation, byte code)
        {
            Payload packet = new BlockChange(POS, item, tool, operation, front, code);
            Ref.Client.Send(packet);
        }
    }
}