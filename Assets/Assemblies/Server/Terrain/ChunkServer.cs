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
using Larnix.Packets.Structs;

namespace Larnix.Server.Terrain
{
    internal class ChunkServer : RefObject<Server>, IDisposable
    {
        private const int CHUNK_SIZE = BlockUtils.CHUNK_SIZE;

        private WorldAPI WorldAPI => Ref<WorldAPI>();
        private BlockDataManager BlockDataManager => Ref<BlockDataManager>();
        private PhysicsManager PhysicsManager => Ref<PhysicsManager>();
        private BlockSender BlockSender => Ref<BlockSender>();

        private readonly Vec2Int _chunkpos;
        private readonly BlockServer[,] _blocksFront = new BlockServer[CHUNK_SIZE, CHUNK_SIZE];
        private readonly BlockServer[,] _blocksBack = new BlockServer[CHUNK_SIZE, CHUNK_SIZE];
        private readonly Dictionary<Vec2Int, StaticCollider> _staticColliders = new();

        public readonly BlockData2[,] ActiveChunkReference;

        public ChunkServer(RefObject<Server> reff, Vec2Int chunkpos) : base(reff)
        {
            _chunkpos = chunkpos;
            ActiveChunkReference = BlockDataManager.ObtainChunkReference(_chunkpos);

            foreach (Vec2Int pos in ChunkIterator.IterateXY())
            {
                Vec2Int POS = BlockUtils.GlobalBlockCoords(_chunkpos, pos);

                BlockData2 blockData = ActiveChunkReference[pos.x, pos.y];

                BlockServer frontBlock = BlockFactory.ConstructBlockObject(POS, blockData.Front, true, WorldAPI);
                _blocksFront[pos.x, pos.y] = frontBlock;

                BlockServer backBlock = BlockFactory.ConstructBlockObject(POS, blockData.Back, false, WorldAPI);
                _blocksBack[pos.x, pos.y] = backBlock;

                RefreshCollider(pos);
            }

            PhysicsManager.SetChunkActive(_chunkpos, true);
        }

        public BlockServer GetBlock(Vec2Int pos, bool isFront)
        {
            return isFront ? _blocksFront[pos.x, pos.y] : _blocksBack[pos.x, pos.y];
        }

        public BlockServer UpdateBlock(Vec2Int pos, bool isFront, BlockData1 block, IWorldAPI.BreakMode breakMode)
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

                _blocksFront[pos.x, pos.y] = BlockFactory.ConstructBlockObject(
                    _blocksFront[pos.x, pos.y].Position,
                    block, true, WorldAPI);
                
                result = _blocksFront[pos.x, pos.y];
            }
            else
            {
                ActiveChunkReference[pos.x, pos.y] = new(
                    front: ActiveChunkReference[pos.x, pos.y].Front,
                    back: block
                );

                _blocksBack[pos.x, pos.y] = BlockFactory.ConstructBlockObject(
                    _blocksBack[pos.x, pos.y].Position,
                    block, false, WorldAPI);

                result = _blocksBack[pos.x, pos.y];
            }

            RefreshCollider(pos);

            BlockData2 newHeader = ((IBinary<BlockData2>)ActiveChunkReference[pos.x, pos.y]).BinaryCopy();

            // Push send update

            if (!((IBinary<BlockData2>)oldHeader).BinaryEquals(newHeader))
            {
                Vec2Int POS = BlockUtils.GlobalBlockCoords(_chunkpos, pos);
                BlockSender.AddBlockUpdate(new BlockUpdateRecord(POS, newHeader, breakMode));
            }

            return result;
        }

        private void RefreshCollider(Vec2Int pos)
        {
            if(_staticColliders.TryGetValue(pos, out var statCollider))
            {
                PhysicsManager.RemoveColliderByReference(statCollider);
                _staticColliders.Remove(pos);
            }

            BlockServer blockServer = _blocksFront[pos.x, pos.y];
            IHasCollider iface = blockServer as IHasCollider;
            if(iface != null)
            {
                StaticCollider staticCollider = StaticCollider.Create(
                    new Vec2(iface.COLLIDER_WIDTH(), iface.COLLIDER_HEIGHT()),
                    new Vec2(iface.COLLIDER_OFFSET_X(), iface.COLLIDER_OFFSET_Y()),
                    BlockUtils.GlobalBlockCoords(_chunkpos, pos)
                    );
                _staticColliders[pos] = staticCollider;
                PhysicsManager.AddCollider(staticCollider);
            }
        }

        public void Dispose()
        {
            foreach(StaticCollider staticCollider in _staticColliders.Values)
            {
                PhysicsManager.RemoveColliderByReference(staticCollider);
            }

            PhysicsManager.SetChunkActive(_chunkpos, false);
            BlockDataManager.ReturnChunkReference(_chunkpos);
        }

#region Frame Invokes

        public void INVOKE_PreFrame() // START
        {
            foreach (Vec2Int pos in ChunkIterator.IterateYX())
            {
                _blocksBack[pos.x, pos.y].PreFrameTrigger();
                _blocksFront[pos.x, pos.y].PreFrameTrigger();
            }
        }

        public void INVOKE_Conway() // 1
        {
            foreach (Vec2Int pos in ChunkIterator.IterateYX())
            {
                _blocksBack[pos.x, pos.y].FrameUpdateConway();
                _blocksFront[pos.x, pos.y].FrameUpdateConway();
            }
        }

        public void INVOKE_SequentialEarly() // 2
        {
            foreach (Vec2Int pos in ChunkIterator.IterateYX())
            {
                _blocksBack[pos.x, pos.y].FrameUpdateSequentialEarly();
                _blocksFront[pos.x, pos.y].FrameUpdateSequentialEarly();
            }
        }

        public void INVOKE_Random() // 3
        {
            foreach (Vec2Int pos in ChunkIterator.IterateRandom())
            {
                _blocksBack[pos.x, pos.y].FrameUpdateRandom();
                _blocksFront[pos.x, pos.y].FrameUpdateRandom();
            }
        }

        public void INVOKE_SequentialLate() // 4
        {
            foreach (Vec2Int pos in ChunkIterator.IterateYX())
            {
                _blocksBack[pos.x, pos.y].FrameUpdateSequentialLate();
                _blocksFront[pos.x, pos.y].FrameUpdateSequentialLate();
            }
        }

        public void INVOKE_PostFrame() // END
        {
            foreach (Vec2Int pos in ChunkIterator.IterateYX())
            {
                _blocksBack[pos.x, pos.y].PostFrameTrigger();
                _blocksFront[pos.x, pos.y].PostFrameTrigger();
            }
        }

#endregion

    }
}
