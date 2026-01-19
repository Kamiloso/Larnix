using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Packets;
using Larnix.Blocks.Structs;
using Larnix.Packets.Game;
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
                BlockData1 block = front ? blockData.Front : blockData.Back;
                BlockData1 frontblock = blockData.Front;

                bool can_replace = BlockFactory.GetSlaveInstance<IReplaceable>(block.ID)?.STATIC_IsReplaceable(block, front) == true;
                bool can_place = BlockFactory.GetSlaveInstance<IPlaceable>(item.ID)?.STATIC_IsPlaceable(item, front) == true;
                bool solid_front = BlockFactory.HasInterface<ISolid>(frontblock.ID);

                return can_replace && can_place && (front || !solid_front);
            }
            else return false;
        }

        public static bool CanBeBroken(Vec2Int POS, BlockData1 tool, bool front)
        {
            BlockData2 blockData = Ref.GridManager.BlockDataAtPOS(POS);
            bool isLocked = Ref.GridManager.IsBlockLocked(POS);

            if (blockData != null && !isLocked)
            {
                BlockData1 block = front ? blockData.Front : blockData.Back;
                BlockData1 frontblock = blockData.Front;

                bool is_breakable = BlockFactory.GetSlaveInstance<IBreakable>(block.ID)?.STATIC_IsBreakable(block, front) == true;
                bool can_mine = BlockFactory.GetSlaveInstance<IBreakable>(block.ID)?.STATIC_CanMineWith(tool) == true;
                bool solid_front = BlockFactory.HasInterface<ISolid>(frontblock.ID);

                return is_breakable && can_mine && (front || !solid_front);
            }
            else return false;
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