using Larnix.Blocks;
using Larnix.Modules.Blocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Packets;
using System;
using QuickNet;
using QuickNet.Channel;

namespace Larnix.Client.Terrain
{
    public static class WorldAPI
    {
        public static bool CanBePlaced(Vector2Int POS, SingleBlockData item, bool front)
        {
            BlockData blockData = References.GridManager.BlockDataAtPOS(POS);
            bool isLocked = References.GridManager.IsBlockLocked(POS);

            if (blockData != null && !isLocked)
            {
                SingleBlockData block = front ? blockData.Front : blockData.Back;
                SingleBlockData frontblock = blockData.Front;

                bool can_replace = BlockFactory.GetSlaveInstance<IReplaceable>(block.ID)?.STATIC_IsReplaceable(block, front) == true;
                bool can_place = BlockFactory.GetSlaveInstance<IPlaceable>(item.ID)?.STATIC_IsPlaceable(item, front) == true;
                bool solid_front = BlockFactory.HasInterface<ISolid>(frontblock.ID);

                return can_replace && can_place && (front || !solid_front);
            }
            else return false;
        }

        public static bool CanBeBroken(Vector2Int POS, SingleBlockData tool, bool front)
        {
            BlockData blockData = References.GridManager.BlockDataAtPOS(POS);
            bool isLocked = References.GridManager.IsBlockLocked(POS);

            if (blockData != null && !isLocked)
            {
                SingleBlockData block = front ? blockData.Front : blockData.Back;
                SingleBlockData frontblock = blockData.Front;

                bool is_breakable = BlockFactory.GetSlaveInstance<IBreakable>(block.ID)?.STATIC_IsBreakable(block, front) == true;
                bool can_mine = BlockFactory.GetSlaveInstance<IBreakable>(block.ID)?.STATIC_CanMineWith(tool) == true;
                bool solid_front = BlockFactory.HasInterface<ISolid>(frontblock.ID);

                return is_breakable && can_mine && (front || !solid_front);
            }
            else return false;
        }

        public static void PlaceBlock(Vector2Int POS, SingleBlockData item, bool front)
        {
            long operation = References.GridManager.PlaceBlockClient(POS, item, front);
            SendBlockChange(POS, item, new(), front, operation, 0);
        }

        public static void BreakBlock(Vector2Int POS, SingleBlockData tool, bool front)
        {
            BlockData _oldblock = References.GridManager.BlockDataAtPOS(POS);
            SingleBlockData oldblock = front ? _oldblock.Front : _oldblock.Back;
            long operation = References.GridManager.BreakBlockClient(POS, front);
            SendBlockChange(POS, oldblock, tool, front, operation, 1);
        }

        private static void SendBlockChange(Vector2Int POS, SingleBlockData item, SingleBlockData tool, bool front, long operation, byte code)
        {
            Packet packet = new BlockChange(POS, item, tool, operation, front, code);
            References.Client.Send(packet);
        }
    }
}