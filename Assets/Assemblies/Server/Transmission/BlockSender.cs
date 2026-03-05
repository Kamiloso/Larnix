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
using Larnix.Core;
using Larnix.Server.Terrain;

namespace Larnix.Server.Transmission
{
    internal record BlockChangeItem(
        string Owner, long Operation, Vec2Int POS, bool Front, bool Success);

    internal class BlockSender : IScript
    {
        private readonly Queue<BlockUpdateRecord> _blockUpdates = new();
        private readonly Queue<BlockChangeItem> _blockChanges = new();

        private IWorldAPI WorldAPI => GlobRef.Get<IWorldAPI>();
        private PlayerActions PlayerActions => GlobRef.Get<PlayerActions>();
        private QuickServer QuickServer => GlobRef.Get<QuickServer>();
        private Chunks Chunks => GlobRef.Get<Chunks>();

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

            foreach (string nickname in PlayerActions.AllPlayers())
            {
                IndividualUpdates[nickname] = new();
            }

            while (_blockUpdates.Count > 0)
            {
                var element = _blockUpdates.Dequeue();
                Vec2Int chunk = BlockUtils.CoordsToChunk(element.Position);

                foreach (string nickname in PlayerActions.AllPlayers())
                {
                    if (PlayerActions.PlayerHasChunk(nickname, chunk))
                        IndividualUpdates[nickname].Enqueue(element);
                }
            }

            foreach (string nickname in PlayerActions.AllPlayers())
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
                    PlayerActions.StateOf(nickname) != Entities.PlayerActions.PlayerState.None &&
                    PlayerActions.PlayerHasChunk(nickname, chunk) &&
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
            foreach (string nickname in PlayerActions.AllPlayers())
            {
                Vec2Int chunkpos = BlockUtils.CoordsToChunk(PlayerActions.RenderingPosition(nickname));
                var player_state = PlayerActions.StateOf(nickname);

                HashSet<Vec2Int> chunksMemory = PlayerActions.LoadedChunksCopy(nickname);
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

                PlayerActions.UpdateClientChunks(nickname, chunksNearby);
            }
        }
    }
}
