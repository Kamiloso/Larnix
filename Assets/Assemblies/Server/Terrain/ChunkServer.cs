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
        private const int CHUNK_SIZE = BlockUtils.CHUNK_SIZE;

        private WorldAPI WorldAPI => Ref<ChunkLoading>().WorldAPI;
        private BlockDataManager BlockDataManager => Ref<BlockDataManager>();
        private PhysicsManager PhysicsManager => Ref<PhysicsManager>();
        private BlockSender BlockSender => Ref<BlockSender>();

        public readonly Vec2Int Chunkpos;
        private readonly BlockServer[,] BlocksFront = new BlockServer[CHUNK_SIZE, CHUNK_SIZE];
        private readonly BlockServer[,] BlocksBack = new BlockServer[CHUNK_SIZE, CHUNK_SIZE];
        private readonly Dictionary<Vec2Int, StaticCollider> StaticColliders = new();

        public readonly BlockData2[,] ActiveChunkReference;

        public ChunkServer(RefObject<Server> reff, Vec2Int chunkpos) : base(reff)
        {
            Chunkpos = chunkpos;
            ActiveChunkReference = BlockDataManager.ObtainChunkReference(Chunkpos);

            foreach (Vec2Int pos in ChunkIterator.IterateXY())
            {
                Vec2Int POS = BlockUtils.GlobalBlockCoords(Chunkpos, pos);

                BlockData2 blockData = ActiveChunkReference[pos.x, pos.y];

                BlockServer frontBlock = BlockFactory.ConstructBlockObject(POS, blockData.Front, true, WorldAPI);
                BlocksFront[pos.x, pos.y] = frontBlock;

                BlockServer backBlock = BlockFactory.ConstructBlockObject(POS, blockData.Back, false, WorldAPI);
                BlocksBack[pos.x, pos.y] = backBlock;

                RefreshCollider(pos);
            }

            PhysicsManager.SetChunkActive(Chunkpos, true);
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

#region Frame Invokes

        public void INVOKE_PreFrame() // START
        {
            foreach (Vec2Int pos in ChunkIterator.IterateYX())
            {
                BlocksBack[pos.x, pos.y].PreFrameTrigger();
                BlocksFront[pos.x, pos.y].PreFrameTrigger();
            }
        }

        public void INVOKE_Conway() // 1
        {
            foreach (Vec2Int pos in ChunkIterator.IterateYX())
            {
                BlocksBack[pos.x, pos.y].FrameUpdateConway();
                BlocksFront[pos.x, pos.y].FrameUpdateConway();
            }
        }

        public void INVOKE_SequentialEarly() // 2
        {
            foreach (Vec2Int pos in ChunkIterator.IterateYX())
            {
                BlocksBack[pos.x, pos.y].FrameUpdateSequentialEarly();
                BlocksFront[pos.x, pos.y].FrameUpdateSequentialEarly();
            }
        }

        public void INVOKE_Random() // 3
        {
            foreach (Vec2Int pos in ChunkIterator.IterateRandom())
            {
                BlocksBack[pos.x, pos.y].FrameUpdateRandom();
                BlocksFront[pos.x, pos.y].FrameUpdateRandom();
            }
        }

        public void INVOKE_SequentialLate() // 4
        {
            foreach (Vec2Int pos in ChunkIterator.IterateYX())
            {
                BlocksBack[pos.x, pos.y].FrameUpdateSequentialLate();
                BlocksFront[pos.x, pos.y].FrameUpdateSequentialLate();
            }
        }

        public void INVOKE_PostFrame() // END
        {
            foreach (Vec2Int pos in ChunkIterator.IterateYX())
            {
                BlocksBack[pos.x, pos.y].PostFrameTrigger();
                BlocksFront[pos.x, pos.y].PostFrameTrigger();
            }
        }

#endregion

    }
}
