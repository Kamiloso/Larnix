using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks;
using System;
using System.Linq;
using Larnix.Core.Utils;
using Larnix.Core.Physics;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using Larnix.Packets.Structs;
using Larnix.Blocks.All;
using Larnix.Core;
using BlockInits = Larnix.Blocks.Block.BlockInits;

namespace Larnix.Server.Terrain
{
    internal class Chunk : IDisposable
    {
        private const int CHUNK_SIZE = BlockUtils.CHUNK_SIZE;

        private readonly Vec2Int _chunkpos;
        private readonly BlockEvents _blockEvents;
        private readonly Block[,] _blocksFront = ChunkIterator.Array2D<Block>();
        private readonly Block[,] _blocksBack = ChunkIterator.Array2D<Block>();
        private readonly Dictionary<Vec2Int, StaticCollider[]> _colliderCollections = new();

        private IWorldAPI WorldAPI => GlobRef.Get<IWorldAPI>();
        private BlockDataManager BlockDataManager => GlobRef.Get<BlockDataManager>();
        private PhysicsManager PhysicsManager => GlobRef.Get<PhysicsManager>();
        private BlockSender BlockSender => GlobRef.Get<BlockSender>();

        public IEnumerable FrameInvoker => _blockEvents.GetFrameInvoker();
        public readonly BlockData2[,] ActiveChunkReference;

        private bool _disposed = false;

        public Chunk(Vec2Int chunkpos)
        {
            _chunkpos = chunkpos;
            _blockEvents = new BlockEvents(_chunkpos, WorldAPI, _blocksFront, _blocksBack);

            ActiveChunkReference = BlockDataManager.ObtainChunkReference(_chunkpos);

            ChunkIterator.Iterate((x, y) =>
            {
                var pos = new Vec2Int(x, y);
                BlockCreate(pos);
                RefreshCollider(pos);
            });

            PhysicsManager.SetChunkActive(_chunkpos, true);
        }

        private void BlockCreate(Vec2Int pos)
        {
            Vec2Int POS = BlockUtils.GlobalBlockCoords(_chunkpos, pos);
            
            BlockData2 blockdata = ActiveChunkReference[pos.x, pos.y];
            Block frontblock = BlockFactory.ConstructBlockObject(
                new BlockInits(POS, true, blockdata.Front, WorldAPI));
            Block backblock = BlockFactory.ConstructBlockObject(
                new BlockInits(POS, false, blockdata.Back, WorldAPI));

            frontblock.AttachTo(_blockEvents);
            backblock.AttachTo(_blockEvents);

            _blocksFront[pos.x, pos.y] = frontblock;
            _blocksBack[pos.x, pos.y] = backblock;
        }

        public Block GetBlock(Vec2Int pos, bool isFront)
        {
            return isFront ? _blocksFront[pos.x, pos.y] : _blocksBack[pos.x, pos.y];
        }

        public Block UpdateBlock(Vec2Int pos, bool isFront, BlockData1 newBlock, IWorldAPI.BreakMode breakMode)
        {
            // --- Chunk changes ---

            BlockData2 oldHeader = ActiveChunkReference[pos.x, pos.y].BinaryCopy();
            Block result = RefreshBlock(pos, newBlock, isFront);
            RefreshCollider(pos);
            BlockData2 newHeader = ActiveChunkReference[pos.x, pos.y].BinaryCopy();

            // --- Rearm EventFlag ---

            if (breakMode == IWorldAPI.BreakMode.Weak)
                result.EventFlag = true;

            // --- Push send update ---

            if (!oldHeader.BinaryEquals(newHeader))
            {
                Vec2Int POS = BlockUtils.GlobalBlockCoords(_chunkpos, pos);
                BlockSender.AddBlockUpdate(new BlockUpdateRecord(
                    POS, newHeader,
                    breakMode
                    ));
            }

            return result;
        }

        public Block UpdateBlockMutated(Vec2Int pos, bool isFront)
        {
            // --- Chunk changes ---

            Block result = GetBlock(pos, isFront);
            RefreshCollider(pos);
            BlockData2 newHeader = ActiveChunkReference[pos.x, pos.y].BinaryCopy();

            // --- Push send update ---

            Vec2Int POS = BlockUtils.GlobalBlockCoords(_chunkpos, pos);
            BlockSender.AddBlockUpdate(new BlockUpdateRecord(
                POS, newHeader,
                IWorldAPI.BreakMode.Replace
                ));

            return result;
        }

        private Block RefreshBlock(Vec2Int pos, BlockData1 block, bool isFront)
        {
            if (isFront)
            {
                ActiveChunkReference[pos.x, pos.y] = new(
                    front: block,
                    back: ActiveChunkReference[pos.x, pos.y].Back
                );

                _blocksFront[pos.x, pos.y].Detach();
                
                _blocksFront[pos.x, pos.y] = BlockFactory.ConstructBlockObject(
                    new BlockInits(_blocksFront[pos.x, pos.y].Position, true, block, WorldAPI));
                
                _blocksFront[pos.x, pos.y].AttachTo(_blockEvents);
                
                return _blocksFront[pos.x, pos.y];
            }
            else
            {
                ActiveChunkReference[pos.x, pos.y] = new(
                    front: ActiveChunkReference[pos.x, pos.y].Front,
                    back: block
                );

                _blocksBack[pos.x, pos.y].Detach();
                
                _blocksBack[pos.x, pos.y] = BlockFactory.ConstructBlockObject(
                    new BlockInits(_blocksBack[pos.x, pos.y].Position, false, block, WorldAPI));

                _blocksBack[pos.x, pos.y].AttachTo(_blockEvents);

                return _blocksBack[pos.x, pos.y];
            }
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

            Block blockServer = _blocksFront[pos.x, pos.y];
            BlockData1 blockData = blockServer.BlockData;

            IHasCollider iface = blockServer as IHasCollider;
            if (iface != null)
            {
                Vec2Int POS = BlockUtils.GlobalBlockCoords(_chunkpos, pos);
                StaticCollider[] staticColliders = iface
                    .STATIC_GetAllColliders(blockData.ID, blockData.Variant)
                    .Select(col => IHasCollider.MakeStaticCollider(col, POS))
                    .ToArray();

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

                foreach (var block in _blocksFront) block.Detach();
                foreach (var block in _blocksBack) block.Detach();

                foreach(var collider in _colliderCollections.Values.SelectMany(x => x))
                {
                    PhysicsManager.RemoveColliderByReference(collider);
                }

                PhysicsManager.SetChunkActive(_chunkpos, false);
                BlockDataManager.ReturnChunkReference(_chunkpos);
            }
        }
    }
}
