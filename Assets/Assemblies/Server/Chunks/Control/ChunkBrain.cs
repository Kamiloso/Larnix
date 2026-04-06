#nullable enable
using System.Collections;
using System.Collections.Generic;
using Larnix.Model.Blocks;
using System;
using System.Linq;
using Larnix.Model.Utils;
using Larnix.Model.Physics;
using Larnix.Core.Vectors;
using Larnix.Model.Blocks.Structs;
using Larnix.Server.Packets.Structs;
using Larnix.Model.Blocks.All;
using Larnix.Core;
using static Larnix.Model.Blocks.Block;
using static Larnix.Model.Interfaces.IWorldAPI;
using Larnix.Model.Interfaces;

namespace Larnix.Server.Chunks.Control;

internal class ChunkBrain : IDisposable
{
    private readonly Vec2Int _chunkpos;
    private readonly BlockEvents _blockEvents;
    private readonly Block[,] _blocksFront = ChunkIterator.Array2D<Block>();
    private readonly Block[,] _blocksBack = ChunkIterator.Array2D<Block>();
    private readonly Dictionary<Vec2Int, StaticCollider[]> _colliderCollections = new();

    public event Action<BlockUpdateRecord>? OnBlockUpdate;

    public IEnumerable FrameInvoker => _blockEvents.GetFrameInvoker();
    public ChunkData ActiveChunkReference { get; }

    private IPhysicsManager PhysicsManager => GlobRef.Get<IPhysicsManager>();
    private IWorldAPI WorldAPI => GlobRef.Get<IWorldAPI>();

    private bool _disposed = false;

    public ChunkBrain(Vec2Int chunkpos, ChunkData chunkActiveReference)
    {
        _chunkpos = chunkpos;
        _blockEvents = new BlockEvents(_chunkpos, WorldAPI, _blocksFront, _blocksBack);

        ActiveChunkReference = chunkActiveReference;

        ChunkIterator.Iterate((x, y) =>
        {
            Vec2Int pos = new(x, y);
            BlockCreate(pos);
            RefreshCollider(pos);
        });

        PhysicsManager.EnableChunk(_chunkpos);
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
        return isFront
            ? _blocksFront[pos.x, pos.y]
            : _blocksBack[pos.x, pos.y];
    }

    public Block UpdateBlock(Vec2Int pos, bool isFront, BlockData1 newBlock, BreakMode breakMode)
    {
        BlockHeader2 oldHeader = ActiveChunkReference[pos.x, pos.y].Header;
        Block result = RefreshBlock(pos, newBlock, isFront);
        RefreshCollider(pos);
        BlockHeader2 newHeader = ActiveChunkReference[pos.x, pos.y].Header;

        if (breakMode == BreakMode.Weak)
        {
            result.EventFlag = true;
        }

        if (oldHeader != newHeader)
        {
            Vec2Int POS = BlockUtils.GlobalBlockCoords(_chunkpos, pos);
            OnBlockUpdate?.Invoke(
                new BlockUpdateRecord(POS, newHeader, breakMode)
                );
        }

        return result;
    }

    public Block UpdateBlockMutated(Vec2Int pos, bool isFront)
    {
        Block result = GetBlock(pos, isFront);
        RefreshCollider(pos);
        BlockHeader2 newHeader = ActiveChunkReference[pos.x, pos.y].Header;

        Vec2Int POS = BlockUtils.GlobalBlockCoords(_chunkpos, pos);
        OnBlockUpdate?.Invoke(
            new BlockUpdateRecord(POS, newHeader, BreakMode.Replace)
            );

        return result;
    }

    private Block RefreshBlock(Vec2Int pos, BlockData1 block, bool isFront)
    {
        BlockData2 chunkRef = ActiveChunkReference[pos.x, pos.y];

        chunkRef = isFront
            ? new BlockData2(block, chunkRef.Back)
            : new BlockData2(chunkRef.Front, block);

        ActiveChunkReference[pos.x, pos.y] = chunkRef;

        var blocks = isFront ? _blocksFront : _blocksBack;

        Block oldBlock = blocks[pos.x, pos.y];
        oldBlock.Detach();

        Block newBlock = BlockFactory.ConstructBlockObject(
            new BlockInits(oldBlock.Position, isFront, block, WorldAPI)
        );

        newBlock.AttachTo(_blockEvents);
        blocks[pos.x, pos.y] = newBlock;

        return newBlock;
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

        IHasCollider? iface = blockServer as IHasCollider;
        if (iface is not null)
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
        if (_disposed) return;
        _disposed = true;

        foreach (var block in _blocksFront)
        {
            block?.Detach();
        }

        foreach (var block in _blocksBack)
        {
            block?.Detach();
        }

        foreach (var collider in _colliderCollections.Values.SelectMany(x => x))
        {
            PhysicsManager.RemoveColliderByReference(collider);
        }

        PhysicsManager.DisableChunk(_chunkpos);
    }
}
