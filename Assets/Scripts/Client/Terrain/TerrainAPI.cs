using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using Larnix.Socket.Packets;
using Larnix.Blocks.Structs;
using Larnix.Packets;
using Larnix.Core.Vectors;
using Larnix.Core;

namespace Larnix.Client.Terrain
{
    public static class TerrainAPI
    {
        private static Client Client => GlobRef.Get<Client>();
        private static GridManager GridManager => GlobRef.Get<GridManager>();

        public static bool CanBePlaced(Vec2Int POS, BlockData1 item, bool front)
        {
            BlockData2 blockData = GridManager.BlockDataAtPOS(POS);
            bool isLocked = GridManager.IsBlockLocked(POS);

            if (blockData != null && !isLocked)
            {
                return BlockInteractions.CanBePlaced(blockData, item, front);
            }
            return false;
        }

        public static bool CanBeBroken(Vec2Int POS, BlockData1 tool, bool front)
        {
            BlockData2 blockData = GridManager.BlockDataAtPOS(POS);
            bool isLocked = GridManager.IsBlockLocked(POS);

            if (blockData != null && !isLocked)
            {
                BlockData1 item = front ? blockData.Front : blockData.Back;
                return BlockInteractions.CanBeBroken(blockData, item, tool, front);
            }
            return false;
        }

        public static void PlaceBlock(Vec2Int POS, BlockData1 item, bool front)
        {
            long operation = GridManager.PlaceBlockClient(POS, item, front);
            SendBlockChange(POS, item, new(), front, operation, 0);
        }

        public static void BreakBlock(Vec2Int POS, BlockData1 tool, bool front)
        {
            BlockData2 _oldblock = GridManager.BlockDataAtPOS(POS);
            BlockData1 oldblock = front ? _oldblock.Front : _oldblock.Back;
            long operation = GridManager.BreakBlockClient(POS, front);
            SendBlockChange(POS, oldblock, tool, front, operation, 1);
        }

        private static void SendBlockChange(Vec2Int POS, BlockData1 item, BlockData1 tool, bool front, long operation, byte code)
        {
            Payload packet = new BlockChange(POS, item, tool, operation, front, code);
            Client.Send(packet);
        }
    }
}