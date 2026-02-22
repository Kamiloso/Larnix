using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks;
using Larnix.Socket.Packets;
using System.Linq;
using Larnix.Core.Utils;
using Larnix.Blocks.Structs;
using Larnix.Server.Entities;
using Larnix.Socket.Backend;
using Larnix.Packets;
using Larnix.Core.Vectors;
using Larnix.Packets.Structs;

namespace Larnix.Server.Terrain
{
    internal record BlockChangeItem(
        string Owner, long Operation, Vec2Int POS, bool Front, bool Success);

    internal class BlockSender : IScript
    {
        private readonly Queue<BlockUpdateRecord> _blockUpdates = new();
        private readonly Queue<BlockChangeItem> _blockChanges = new();

        private IWorldAPI WorldAPI => Ref.IWorldAPI;
        private PlayerManager PlayerManager => Ref.PlayerManager;
        private QuickServer QuickServer => Ref.QuickServer;
        private Chunks Chunks => Ref.Chunks;

        public void AddBlockUpdate(BlockUpdateRecord element)
        {
            _blockUpdates.Enqueue(element);
        }

        public void AddRetBlockChange(BlockChangeItem element)
        {
            _blockChanges.Enqueue(element);
        }

        void IScript.PostLateFrameUpdate()
        {
            SendBlockUpdate(); // common block updates (server / players)
            SendBlockChanges(); // unlocking client-side & telling the real block state
        }

        private void SendBlockUpdate()
        {
            Dictionary<string, Queue<BlockUpdateRecord>> IndividualUpdates = new();

            foreach (string nickname in PlayerManager.AllPlayers())
            {
                IndividualUpdates[nickname] = new();
            }

            while (_blockUpdates.Count > 0)
            {
                var element = _blockUpdates.Dequeue();
                Vec2Int chunk = BlockUtils.CoordsToChunk(element.Position);

                foreach (string nickname in PlayerManager.AllPlayers())
                {
                    if (PlayerManager.PlayerHasChunk(nickname, chunk))
                        IndividualUpdates[nickname].Enqueue(element);
                }
            }

            foreach (string nickname in PlayerManager.AllPlayers())
            {
                Queue<BlockUpdateRecord> changes = IndividualUpdates[nickname];
                BlockUpdateRecord[] records = changes.ToArray();

                List<BlockUpdate> packets = BlockUpdate.CreateList(records);
                foreach (Payload packet in packets)
                {
                    QuickServer.Send(nickname, packet);
                }
            }
        }

        private void SendBlockChanges()
        {
            while(_blockChanges.Count > 0)
            {
                BlockChangeItem element = _blockChanges.Dequeue();

                string nickname = element.Owner;
                Vec2Int POS = element.POS;
                Vec2Int chunk = BlockUtils.CoordsToChunk(POS);
                bool front = element.Front;
                bool success = element.Success;
                long operation = element.Operation;

                Block blockFront = WorldAPI.GetBlock(POS, true);
                Block blockBack = WorldAPI.GetBlock(POS, false);

                if (
                    PlayerManager.StateOf(nickname) != Entities.PlayerManager.PlayerState.None &&
                    PlayerManager.PlayerHasChunk(nickname, chunk) &&
                    blockFront != null && blockBack != null
                    )
                {
                    BlockData2 currentBlock = new BlockData2(blockFront.BlockData, blockBack.BlockData);

                    Payload packet = new RetBlockChange(POS, operation, currentBlock, front, success);
                    QuickServer.Send(nickname, packet);
                }
            }
        }

        public void BroadcastChunkChanges()
        {
            foreach (string nickname in PlayerManager.AllPlayers())
            {
                Vec2Int chunkpos = BlockUtils.CoordsToChunk(PlayerManager.RenderingPosition(nickname));
                var player_state = PlayerManager.StateOf(nickname);

                HashSet<Vec2Int> chunksMemory = PlayerManager.LoadedChunksCopy(nickname);
                HashSet<Vec2Int> chunksNearby = BlockUtils.GetNearbyChunks(chunkpos, BlockUtils.LOADING_DISTANCE)
                    .Where(c => Chunks.IsChunkLoaded(c))
                    .ToHashSet();

                HashSet<Vec2Int> added = new(chunksNearby);
                added.ExceptWith(chunksMemory);

                HashSet<Vec2Int> removed = new(chunksMemory);
                removed.ExceptWith(chunksNearby);

                // Send added
                foreach (var chunk in added)
                {
                    BlockData2[,] chunkArray = Chunks.GetChunk(chunk).ActiveChunkReference;
                    Payload packet = new ChunkInfo(chunk, chunkArray);
                    QuickServer.Send(nickname, packet);
                }

                // Send removed
                foreach (var chunk in removed)
                {
                    Payload packet = new ChunkInfo(chunk, null);
                    QuickServer.Send(nickname, packet);
                }

                PlayerManager.UpdateClientChunks(nickname, chunksNearby);
            }
        }
    }
}
