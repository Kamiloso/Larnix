using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Blocks;
using System.Linq;
using System;
using Larnix.GameCore.Utils;
using Larnix.GameCore.Physics;
using Larnix.Core;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using Larnix.Client.Particles;
using Larnix.GameCore.Enums;
using Larnix.Core.Misc;
using IHasCollider = Larnix.Blocks.All.IHasCollider;
using Larnix.Core.Enums;
using Larnix.GameCore.Structs;

namespace Larnix.Client.Terrain
{
    public class GridManager : BasicGridManager
    {
        private const double LOCK_TIMEOUT = 5.0; // seconds

        private readonly Dictionary<Vec2Int, StaticCollider[]> _colliderCollections = new();
        private readonly List<BlockLock> _lockedBlocks = new();

        private ParticleManager ParticleManager => GlobRef.Get<ParticleManager>();
        private PhysicsManager PhysicsManager => GlobRef.Get<PhysicsManager>();

        private class BlockLock
        {
            public Vec2Int POS;
            public long operation;
            public double timeout = LOCK_TIMEOUT;
        }

        protected override void Awake()
        {
            base.Awake();

            GlobRef.Set(this);

            OnChunkChanged += UpdateChunkColliders;
            OnChunkRemoved += UnlockChunk;
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

        public void UpdateBlock(Vec2Int POS, BlockHeader2 data, IWorldAPI.BreakMode breakMode, long? unlock = null)
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

        public long PlaceBlockClient(Vec2Int POS, BlockHeader1 block, bool front)
        {
            BlockHeader2? oldblockNullable = BlockDataAtPOS(POS);
            if (oldblockNullable == null)
                throw new InvalidOperationException($"Cannot place block at {POS}");

            BlockHeader2 oldBlock = oldblockNullable.Value;

            BlockHeader2 blockdata = new(
                front ? block : oldBlock.Front,
                front ? oldBlock.Back : block
                );

            ChangeBlockData(POS, blockdata, IWorldAPI.BreakMode.Effects);
            long operation = LockBlock(POS);
            return operation;
        }

        public long BreakBlockClient(Vec2Int POS, bool front)
        {
            BlockHeader2? oldBlockNullable = BlockDataAtPOS(POS);
            if (oldBlockNullable == null)
                throw new InvalidOperationException($"Cannot break block at {POS}");

            BlockHeader2 oldBlock = oldBlockNullable.Value;

            BlockHeader2 blockdata = new(
                front ? BlockHeader1.Air : oldBlock.Front,
                front ? oldBlock.Back : BlockHeader1.Air
                );

            ChangeBlockData(POS, blockdata, IWorldAPI.BreakMode.Effects);
            long operation = LockBlock(POS);
            return operation;
        }

        private void ChangeBlockData(Vec2Int POS, BlockHeader2 newBlock, IWorldAPI.BreakMode breakMode)
        {
            Vec2Int chunk = BlockUtils.CoordsToChunk(POS);
            Vec2Int pos = BlockUtils.LocalBlockCoords(POS);

            BlockHeader2 oldBlock = _allChunks[chunk][pos.x, pos.y];
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

        private void UpdateChunkColliders(Vec2Int chunk)
        {
            if (!_allChunks.TryGetValue(chunk, out ChunkView chunkView))
                chunkView = null;

            ChunkIterator.Iterate((x, y) =>
            {
                var pos = new Vec2Int(x, y);

                Vec2Int POS = BlockUtils.GlobalBlockCoords(chunk, pos);
                UpdateBlockCollider(POS, chunkView?[x, y]);
            });

            PhysicsManager.SetChunkActive(chunk, chunkView != null);
        }

        private void UpdateBlockCollider(Vec2Int POS, BlockHeader2? blockNullable)
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

            if (blockNullable != null)
            {
                BlockHeader2 block = (BlockHeader2)blockNullable;

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
            long operation = RandUtils.NextLong();
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

        private void UnlockChunk(Vec2Int chunk)
        {
            _lockedBlocks.RemoveAll(l => BlockUtils.InChunk(chunk, l.POS));
        }
    }
}
