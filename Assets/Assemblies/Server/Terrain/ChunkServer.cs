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
        private readonly Dictionary<Vec2Int, IEnumerable<StaticCollider>> _colliderCollections = new();

        public readonly BlockData2[,] ActiveChunkReference;

        private bool _disposed = false;

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

            // Reflag for frame events
            
            if (breakMode == IWorldAPI.BreakMode.Weak)
            {
                _blocksFront[pos.x, pos.y].ReflagForEvents();
                _blocksBack[pos.x, pos.y].ReflagForEvents();
            }

            return result;
        }

        private void RefreshCollider(Vec2Int pos)
        {
            { // free old colliders
                if (_colliderCollections.TryGetValue(pos, out var staticColliders))
                {
                    foreach (var collider in staticColliders)
                    {
                        PhysicsManager.RemoveColliderByReference(collider);
                    }
                    _colliderCollections.Remove(pos);
                }
            }

            BlockServer blockServer = _blocksFront[pos.x, pos.y];
            BlockData1 blockData = blockServer.BlockData;

            IHasCollider iface = blockServer as IHasCollider;
            if (iface != null)
            {
                Vec2Int POS = BlockUtils.GlobalBlockCoords(_chunkpos, pos);
                IEnumerable<StaticCollider> staticColliders = iface
                    .STATIC_GetAllColliders(blockData.ID, blockData.Variant)
                    .Select(col => IHasCollider.MakeStaticCollider(col, POS))
                    .ToList();

                _colliderCollections.Add(pos, staticColliders);
                foreach (var collider in staticColliders)
                {
                    PhysicsManager.AddCollider(collider);
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                foreach(var collider in _colliderCollections.Values.SelectMany(x => x))
                {
                    PhysicsManager.RemoveColliderByReference(collider);
                }

                PhysicsManager.SetChunkActive(_chunkpos, false);
                BlockDataManager.ReturnChunkReference(_chunkpos);
            }
        }

#region Frame Invokes

        public void INVOKE_PreFrame() // START 1
        {
            foreach (Vec2Int pos in ChunkIterator.IterateYX())
            {
                _blocksBack[pos.x, pos.y].PreFrameTrigger();
                _blocksFront[pos.x, pos.y].PreFrameTrigger();
            }
        }

        public void INVOKE_PreFrameSelfMutations() // START 2
        {
            foreach (Vec2Int pos in ChunkIterator.IterateYX())
            {
                _blocksBack[pos.x, pos.y].PreFrameTriggerSelfMutations();
                _blocksFront[pos.x, pos.y].PreFrameTriggerSelfMutations();
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

        public void INVOKE_Sequential() // 2
        {
            foreach (Vec2Int pos in ChunkIterator.IterateYX())
            {
                _blocksBack[pos.x, pos.y].FrameUpdateSequential();
                _blocksFront[pos.x, pos.y].FrameUpdateSequential();
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

        public void INVOKE_ElectricPropagation() // 4
        {
            foreach (Vec2Int pos in ChunkIterator.IterateYX())
            {
                _blocksBack[pos.x, pos.y].FrameUpdateElectricPropagation();
                _blocksFront[pos.x, pos.y].FrameUpdateElectricPropagation();
            }
        }

        public void INVOKE_ElectricFinalize() // 5
        {
            foreach (Vec2Int pos in ChunkIterator.IterateYX())
            {
                _blocksBack[pos.x, pos.y].FrameUpdateElectricFinalize();
                _blocksFront[pos.x, pos.y].FrameUpdateElectricFinalize();
            }
        }

        public void INVOKE_SequentialLate() // 6
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
