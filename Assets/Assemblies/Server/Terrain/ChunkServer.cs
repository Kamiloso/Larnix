using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks;
using System;
using System.Linq;
using Larnix.Core.Utils;
using Larnix.Core.Physics;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using Larnix.Core.References;
using Larnix.Core.Binary;

namespace Larnix.Server.Terrain
{
    internal class ChunkServer : RefObject<Server>, IDisposable
    {
        private WorldAPI WorldAPI => Ref<ChunkLoading>().WorldAPI;
        private BlockDataManager BlockDataManager => Ref<BlockDataManager>();
        private PhysicsManager PhysicsManager => Ref<PhysicsManager>();
        private BlockSender BlockSender => Ref<BlockSender>();

        public readonly Vec2Int Chunkpos;
        private readonly BlockServer[,] BlocksFront = new BlockServer[16, 16];
        private readonly BlockServer[,] BlocksBack = new BlockServer[16, 16];
        private readonly Dictionary<Vec2Int, StaticCollider> StaticColliders = new();

        public readonly BlockData2[,] ActiveChunkReference;

        public ChunkServer(RefObject<Server> reff, Vec2Int chunkpos) : base(reff)
        {
            Chunkpos = chunkpos;
            ActiveChunkReference = BlockDataManager.ObtainChunkReference(Chunkpos);

            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    Vec2Int pos = new Vec2Int(x, y);
                    Vec2Int POS = BlockUtils.GlobalBlockCoords(Chunkpos, pos);

                    BlockData2 blockData = ActiveChunkReference[x, y];

                    BlockServer frontBlock = BlockFactory.ConstructBlockObject(POS, blockData.Front, true, WorldAPI);
                    BlocksFront[pos.x, pos.y] = frontBlock;

                    BlockServer backBlock = BlockFactory.ConstructBlockObject(POS, blockData.Back, false, WorldAPI);
                    BlocksBack[pos.x, pos.y] = backBlock;

                    RefreshCollider(pos);
                }

            PhysicsManager.SetChunkActive(Chunkpos, true);
        }

        public void PreExecuteFrame() // 1.
        {
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    BlocksBack[x, y].PreFrameTrigger();
                    BlocksFront[x, y].PreFrameTrigger();
                }
        }

        public void ExecuteFrameRandom() // 2.
        {
            foreach (var pos in Common.GetShuffledPositions(16, 16))
            {
                BlocksBack[pos.x, pos.y].FrameUpdateRandom();
                BlocksFront[pos.x, pos.y].FrameUpdateRandom();
            }
        }

        public void ExecuteFrameSequential() // 3.
        {
            // Back update
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    BlocksBack[x, y].FrameUpdateSequential();
                }

            // Front update
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    BlocksFront[x, y].FrameUpdateSequential();
                }
        }

        public BlockServer GetBlock(Vec2Int pos, bool isFront)
        {
            return isFront ? BlocksFront[pos.x, pos.y] : BlocksBack[pos.x, pos.y];
        }

        public BlockServer UpdateBlock(Vec2Int pos, bool isFront, BlockData1 block)
        {
            BlockServer result;

            // Change blocks

            BlockData2 oldHeader = ((IBinary<BlockData2>)ActiveChunkReference[pos.x, pos.y]).BinaryCopy();

            if (isFront)
            {
                ActiveChunkReference[pos.x, pos.y] = new(
                    front: block,
                    back: ActiveChunkReference[pos.x, pos.y].Back
                );

                BlocksFront[pos.x, pos.y] = BlockFactory.ConstructBlockObject(
                    BlocksFront[pos.x, pos.y].Position,
                    block, true, WorldAPI);
                
                result = BlocksFront[pos.x, pos.y];
            }
            else
            {
                ActiveChunkReference[pos.x, pos.y] = new(
                    front: ActiveChunkReference[pos.x, pos.y].Front,
                    back: block
                );

                BlocksBack[pos.x, pos.y] = BlockFactory.ConstructBlockObject(
                    BlocksBack[pos.x, pos.y].Position,
                    block, false, WorldAPI);

                result = BlocksBack[pos.x, pos.y];
            }

            RefreshCollider(pos);

            BlockData2 newHeader = ((IBinary<BlockData2>)ActiveChunkReference[pos.x, pos.y]).BinaryCopy();

            // Push send update

            if (!((IBinary<BlockData2>)oldHeader).BinaryEquals(newHeader))
            {
                Vec2Int POS = BlockUtils.GlobalBlockCoords(Chunkpos, pos);
                BlockSender.AddBlockUpdate((POS, newHeader));
            }

            return result;
        }

        private void RefreshCollider(Vec2Int pos)
        {
            if(StaticColliders.TryGetValue(pos, out var statCollider))
            {
                PhysicsManager.RemoveColliderByReference(statCollider);
                StaticColliders.Remove(pos);
            }

            BlockServer blockServer = BlocksFront[pos.x, pos.y];
            IHasCollider iface = blockServer as IHasCollider;
            if(iface != null)
            {
                StaticCollider staticCollider = StaticCollider.Create(
                    new Vec2(iface.COLLIDER_WIDTH(), iface.COLLIDER_HEIGHT()),
                    new Vec2(iface.COLLIDER_OFFSET_X(), iface.COLLIDER_OFFSET_Y()),
                    BlockUtils.GlobalBlockCoords(Chunkpos, pos)
                    );
                StaticColliders[pos] = staticCollider;
                PhysicsManager.AddCollider(staticCollider);
            }
        }

        public void Dispose()
        {
            foreach(StaticCollider staticCollider in StaticColliders.Values)
            {
                PhysicsManager.RemoveColliderByReference(staticCollider);
            }

            PhysicsManager.SetChunkActive(Chunkpos, false);
            BlockDataManager.ReturnChunkReference(Chunkpos);
        }
    }
}
