using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Blocks;
using System.Linq;
using System;
using Larnix.Core.Utils;
using Larnix.Core.Physics;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using Larnix.Core;
using Larnix.Client.Particles;
using Random = System.Random;
using IHasCollider = Larnix.Blocks.All.IHasCollider;

namespace Larnix.Client.Terrain
{
    public class GridManager : BasicGridManager
    {
        private const double LOCK_TIMEOUT = 5.0; // seconds

        private readonly Dictionary<Vec2Int, StaticCollider[]> _colliderCollections = new();
        private readonly List<BlockLock> _lockedBlocks = new();

        private ParticleManager ParticleManager => Ref.ParticleManager;
        private PhysicsManager PhysicsManager => Ref.PhysicsManager;

        private class BlockLock
        {
            public Vec2Int POS;
            public long operation;
            public double timeout = LOCK_TIMEOUT;
        }

        protected override void Awake()
        {
            base.Awake();

            if (!IsMenu)
                Ref.GridManager = this;
        }

        protected override void Update()
        {
            base.Update();

            foreach (var l in _lockedBlocks)
                l.timeout -= Time.deltaTime;

            _lockedBlocks.RemoveAll(l => l.timeout < 0);
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
        }

        public void UpdateBlock(Vec2Int POS, BlockData2 data, IWorldAPI.BreakMode breakMode, long? unlock = null)
        {
            Vec2Int chunk = BlockUtils.CoordsToChunk(POS);
            Vec2Int pos = BlockUtils.LocalBlockCoords(POS);

            if (unlock != null)
                UnlockBlock((long)unlock);

            if (_allChunks.ContainsKey(chunk) && !IsBlockLocked(POS))
                ChangeBlockData(POS, data, breakMode);
        }

        public bool LoadedAroundPlayer()
        {
            HashSet<Vec2Int> nearbyChunks = BlockUtils.GetNearbyChunks(
                BlockUtils.CoordsToChunk(MainPlayer.Position),
                BlockUtils.LOADING_DISTANCE
                );

            nearbyChunks.ExceptWith(_visibleChunks);
            return nearbyChunks.Count == 0;
        }

        public long PlaceBlockClient(Vec2Int POS, BlockData1 block, bool front)
        {
            BlockData2 oldblock = BlockDataAtPOS(POS);
            if (oldblock == null)
                throw new InvalidOperationException($"Cannot place block at {POS}");

            BlockData2 blockdata = new BlockData2(
                front ? block : oldblock.Front.DeepCopy(),
                front ? oldblock.Back.DeepCopy() : block
                );

            ChangeBlockData(POS, blockdata, IWorldAPI.BreakMode.Effects);
            long operation = LockBlock(POS);
            return operation;
        }

        public long BreakBlockClient(Vec2Int POS, bool front)
        {
            BlockData2 oldblock = BlockDataAtPOS(POS);
            if (oldblock == null)
                throw new InvalidOperationException($"Cannot break block at {POS}");

            BlockData2 blockdata = new BlockData2(
                front ? new BlockData1 { } : oldblock.Front.DeepCopy(),
                front ? oldblock.Back.DeepCopy() : new BlockData1 { }
                );

            ChangeBlockData(POS, blockdata, IWorldAPI.BreakMode.Effects);
            long operation = LockBlock(POS);
            return operation;
        }

        private void ChangeBlockData(Vec2Int POS, BlockData2 newBlock, IWorldAPI.BreakMode breakMode)
        {
            Vec2Int chunk = BlockUtils.CoordsToChunk(POS);
            Vec2Int pos = BlockUtils.LocalBlockCoords(POS);

            BlockData2 oldBlock = _allChunks[chunk][pos.x, pos.y];
            _allChunks[chunk][pos.x, pos.y] = newBlock;

            if (breakMode == IWorldAPI.BreakMode.Effects)
            {
                if (oldBlock.Front.ID == BlockID.Air && newBlock.Front.ID != BlockID.Air) // front block placed
                    ParticleManager.SpawnBlockParticles(newBlock.Front, POS, true, ParticleID.BlockPlace);

                if (oldBlock.Front.ID != BlockID.Air && newBlock.Front.ID == BlockID.Air) // front block broken
                    ParticleManager.SpawnBlockParticles(oldBlock.Front, POS, true, ParticleID.BlockBreak);

                if (oldBlock.Back.ID == BlockID.Air && newBlock.Back.ID != BlockID.Air) // back block placed
                    ParticleManager.SpawnBlockParticles(newBlock.Back, POS, false, ParticleID.BlockPlace);

                if (oldBlock.Back.ID != BlockID.Air && newBlock.Back.ID == BlockID.Air) // back block broken
                    ParticleManager.SpawnBlockParticles(oldBlock.Back, POS, false, ParticleID.BlockBreak);
            }

            RedrawTileChecked(chunk, pos, newBlock);
            UpdateBlockCollider(POS, newBlock);
        }

        public void UpdateChunkColliders(Vec2Int chunk)
        {
            if (!_allChunks.TryGetValue(chunk, out BlockData2[,] chunkBlocks))
                chunkBlocks = null;

            ChunkIterator.Iterate((x, y) =>
            {
                var pos = new Vec2Int(x, y);

                Vec2Int POS = BlockUtils.GlobalBlockCoords(chunk, pos);
                UpdateBlockCollider(POS, chunkBlocks != null ? chunkBlocks[x, y] : null);
            });

            PhysicsManager.SetChunkActive(chunk, chunkBlocks != null);
        }

        private void UpdateBlockCollider(Vec2Int POS, BlockData2 block)
        {
            { // free old collider
                if (_colliderCollections.TryGetValue(POS, out var staticColliders))
                {
                    foreach (var collider in staticColliders)
                    {
                        PhysicsManager.RemoveColliderByReference(collider);
                    }
                    _colliderCollections.Remove(POS);
                }
            }

            if (block != null)
            {
                IHasCollider iface = BlockFactory.GetSlaveInstance<IHasCollider>(block.Front.ID);
                if (iface != null)
                {
                    StaticCollider[] staticColliders = iface
                        .STATIC_GetAllColliders(block.Front.ID, block.Front.Variant)
                        .Select(col => IHasCollider.MakeStaticCollider(col, POS))
                        .ToArray();

                    _colliderCollections.Add(POS, staticColliders);
                    foreach (var collider in staticColliders)
                    {
                        PhysicsManager.AddCollider(collider);
                    }
                }
            }
        }

        public bool IsBlockLocked(Vec2Int POS)
        {
            return _lockedBlocks.Any(l => l.POS == POS);
        }

        private long LockBlock(Vec2Int POS)
        {
            Random Rand = Common.Rand();
            long operation = ((long)Rand.Next(int.MinValue, int.MaxValue) << 32) | (uint)Rand.Next(int.MinValue, int.MaxValue);
            _lockedBlocks.Add(new BlockLock
            {
                POS = POS,
                operation = operation,
            });
            return operation;
        }

        private void UnlockBlock(long operation)
        {
            _lockedBlocks.RemoveAll(l => l.operation == operation);
        }

        public void UnlockChunk(Vec2Int chunk)
        {
            _lockedBlocks.RemoveAll(l => BlockUtils.InChunk(chunk, l.POS));
        }
    }
}
