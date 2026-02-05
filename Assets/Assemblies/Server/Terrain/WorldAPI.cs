using Larnix.Blocks;
using Larnix.Core.Utils;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using Larnix.Socket.Packets;
using Larnix.Packets;
using Larnix.Core;
using Larnix.Server.Entities;
using System.Linq;
using Larnix.Socket.Backend;
using Larnix.Core.Binary;

namespace Larnix.Server.Terrain
{
    internal class WorldAPI : Singleton, IWorldAPI
    {
        private ChunkLoading ChunkLoading => Ref<ChunkLoading>();

        public uint FramesSinceServerStart() => Ref<Server>().FixedFrame;

        public WorldAPI(Server server) : base(server) { }

        public BlockServer GetBlock(Vec2Int POS, bool isFront)
        {
            Vec2Int chunk = BlockUtils.CoordsToChunk(POS);
            if (ChunkLoading.TryGetChunk(chunk, out var chunkObject))
            {
                Vec2Int pos = BlockUtils.LocalBlockCoords(POS);
                return chunkObject.GetBlock(pos, isFront);
            }
            return null;
        }

        public BlockServer ReplaceBlock(Vec2Int POS, bool isFront, BlockData1 blockTemplate, IWorldAPI.BreakMode breakMode)
        {
            Vec2Int chunk = BlockUtils.CoordsToChunk(POS);
            if (ChunkLoading.TryGetChunk(chunk, out var chunkObject))
            {
                Vec2Int pos = BlockUtils.LocalBlockCoords(POS);
                BlockData1 blockDeepCopy = blockTemplate.DeepCopy();
                return chunkObject.UpdateBlock(pos, isFront, blockDeepCopy, breakMode);
            }
            return null;
        }

        public BlockServer SetBlockVariant(Vec2Int POS, bool isFront, byte variant)
        {
            BlockData1 oldBlock = GetBlock(POS, isFront)?.BlockData;
            if (oldBlock == null) return null;

            BlockData1 blockTemplate = new BlockData1(
                id: oldBlock.ID,
                variant: variant,
                data: oldBlock.Data);
            
            return ReplaceBlock(POS, isFront, blockTemplate, IWorldAPI.BreakMode.Replace);
        }

        public bool CanPlaceBlock(Vec2Int POS, bool front, BlockData1 item)
        {
            BlockData1 frontBlock = GetBlock(POS, true)?.BlockData;
            BlockData1 backBlock = GetBlock(POS, false)?.BlockData;

            if (frontBlock != null && backBlock != null)
            {
                BlockData1 block = front ? frontBlock : backBlock;

                bool can_replace = BlockFactory.GetSlaveInstance<IReplaceable>(block.ID)?.STATIC_IsReplaceable(block, front) == true;
                bool can_place = BlockFactory.GetSlaveInstance<IPlaceable>(item.ID)?.STATIC_IsPlaceable(item, front) == true;
                bool solid_front = BlockFactory.HasInterface<ISolid>(frontBlock.ID);

                return can_replace && can_place && (front || !solid_front);
            }
            else return false;
        }

        public bool CanBreakBlock(Vec2Int POS, bool front, BlockData1 item, BlockData1 tool)
        {
            BlockData1 frontBlock = GetBlock(POS, true)?.BlockData;
            BlockData1 backBlock = GetBlock(POS, false)?.BlockData;

            if (frontBlock != null && backBlock != null)
            {
                BlockData1 block = front ? frontBlock : backBlock;

                if (!((IBinary<BlockData1>)block).BinaryEquals(item))
                    return false;

                bool is_breakable = BlockFactory.GetSlaveInstance<IBreakable>(block.ID)?.STATIC_IsBreakable(block, front) == true;
                bool can_mine = BlockFactory.GetSlaveInstance<IBreakable>(block.ID)?.STATIC_CanMineWith(tool) == true;
                bool solid_front = BlockFactory.HasInterface<ISolid>(frontBlock.ID);

                return is_breakable && can_mine && (front || !solid_front);
            }
            else return false;
        }

        public void PlaceBlockWithEffects(Vec2Int POS, bool front, BlockData1 item)
        {
            ReplaceBlock(POS, front, item, IWorldAPI.BreakMode.Effects);
        }

        public void BreakBlockWithEffects(Vec2Int POS, bool front, BlockData1 tool)
        {
            BlockData1 oldBlock = GetBlock(POS, front)?.BlockData;
            if (oldBlock == null) return;

            // DROP ITEMS
            // INSERT LATER

            ReplaceBlock(POS, front, new BlockData1(), IWorldAPI.BreakMode.Effects);
        }
    }
}
