#nullable enable
using System.Collections.Generic;
using Larnix.Socket.Packets;
using System.Linq;
using Larnix.Model.Utils;
using Larnix.Model.Blocks.Structs;
using Larnix.Server.Packets;
using Larnix.Core.Vectors;
using Larnix.Server.Packets.Structs;
using Larnix.Core;
using Larnix.Server.Entities;
using Larnix.Model.Blocks;

namespace Larnix.Server.Chunks.Scripts;

internal record BlockChangeItem(
    string Nickname, long Operation, Vec2Int POS, bool Front, bool Success);

internal interface IChunkSender : IScript
{
    void AddRetBlockChange(BlockChangeItem elm);
}

internal class ChunkSender : IChunkSender
{
    private readonly Queue<BlockUpdateRecord> _blockUpdates = new();
    private readonly Queue<BlockChangeItem> _blockChanges = new();

    private IServer Server => GlobRef.Get<IServer>();
    private IChunkHolders ChunkHolders => GlobRef.Get<IChunkHolders>();
    private IConnectedPlayers ConnectedPlayers => GlobRef.Get<IConnectedPlayers>();
    private IWorldAPI WorldAPI => GlobRef.Get<IWorldAPI>();

    public void AddRetBlockChange(BlockChangeItem elm) => _blockChanges.Enqueue(elm);

    public ChunkSender()
    {
        ChunkHolders.OnFullyLoaded += chunk =>
        {
            ChunkHolders.GetChunkBrain(chunk)!.OnBlockUpdate +=
                blockUpdateRecord => _blockUpdates.Enqueue(blockUpdateRecord);
        };
    }

    void IScript.PostLateFrameUpdate()
    {
        BroadcastChunkChanges(); // updating chunks loaded by players
        SendBlockUpdate(); // common block updates (server / players)
        SendBlockChanges(); // unlocking client-side & telling the real block state
    }

    private void BroadcastChunkChanges()
    {
        foreach (string nickname in ConnectedPlayers.AllPlayers)
        {
            Vec2Int chunkpos = BlockUtils.CoordsToChunk(ConnectedPlayers[nickname].RenderPosition);

            HashSet<Vec2Int> chunksMemory = ConnectedPlayers[nickname].LoadedChunks;
            HashSet<Vec2Int> chunksNearby = BlockUtils.GetNearbyChunks(chunkpos, BlockUtils.LOADING_DISTANCE)
                .Where(chunk => ChunkHolders.IsChunkInZone(chunk, ChunkLoadState.Loaded))
                .ToHashSet();

            HashSet<Vec2Int> added = new(chunksNearby);
            added.ExceptWith(chunksMemory);

            HashSet<Vec2Int> removed = new(chunksMemory);
            removed.ExceptWith(chunksNearby);

            if (added.Count == 0 && removed.Count == 0)
                continue;

            foreach (var chunk in added)
            {
                ChunkView chunkData = ChunkHolders.GetChunkBrain(chunk)!.ActiveChunkReference.HeaderView;
                Payload_Legacy packet = new ChunkInfo(chunk, chunkData);
                Server.Send(nickname, packet);
            }

            foreach (var chunk in removed)
            {
                Payload_Legacy packet = new ChunkInfo(chunk, null);
                Server.Send(nickname, packet);
            }

            ConnectedPlayers[nickname].LoadedChunks = chunksNearby;
        }
    }

    private void SendBlockUpdate()
    {
        var individualUpdates = ConnectedPlayers.AllPlayers
            .ToDictionary(nickname => nickname, _ => new Queue<BlockUpdateRecord>());

        while (_blockUpdates.Count > 0)
        {
            var elm = _blockUpdates.Dequeue();
            Vec2Int chunk = BlockUtils.CoordsToChunk(elm.Position);

            foreach (string nickname in ConnectedPlayers.AllPlayers)
            {
                if (ConnectedPlayers[nickname].LoadedChunks.Contains(chunk))
                {
                    individualUpdates[nickname].Enqueue(elm);
                }
            }
        }

        foreach (string nickname in ConnectedPlayers.AllPlayers)
        {
            var changes = individualUpdates[nickname];
            BlockUpdateRecord[] records = changes.ToArray();

            List<BlockUpdate> packets = BlockUpdate.CreateList(records);
            foreach (Payload_Legacy packet in packets)
            {
                Server.Send(nickname, packet);
            }
        }
    }

    private void SendBlockChanges()
    {
        while (_blockChanges.TryDequeue(out var elm))
        {
            Vec2Int POS = elm.POS;
            Vec2Int chunk = BlockUtils.CoordsToChunk(POS);

            BlockData1? blockFront = WorldAPI.GetBlock(POS, true)?.BlockData;
            BlockData1? blockBack = WorldAPI.GetBlock(POS, false)?.BlockData;

            if (blockFront is not null && blockBack is not null &&
                ConnectedPlayers[elm.Nickname].LoadedChunks.Contains(chunk))
            {
                BlockHeader2 currentBlock = new(blockFront.Header, blockBack.Header);
                Payload_Legacy packet = new RetBlockChange(POS, elm.Operation, currentBlock, elm.Front, elm.Success);
                Server.Send(elm.Nickname, packet);
            }
        }
    }
}
