using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Blocks;
using Larnix.Socket.Packets;
using System.Linq;
using Larnix.Core.Utils;
using Larnix.Blocks.Structs;
using Larnix.Socket.Structs;
using Larnix.Server.Entities;
using Larnix.Socket.Backend;
using Larnix.Server.References;

namespace Larnix.Server.Terrain
{
    internal class BlockSender : ServerSingleton
    {
        private WorldAPI WorldAPI => Ref<ChunkLoading>().WorldAPI;

        private readonly Queue<(Vector2Int block, BlockData2 data)> BlockUpdates = new();
        private readonly Queue<(string owner, long operation, Vector2Int POS, bool front, bool success)> BlockChanges = new();

        public BlockSender(Server server) : base(server) {}

        public void AddBlockUpdate((Vector2Int, BlockData2) element)
        {
            BlockUpdates.Enqueue(element);
        }

        public void AddRetBlockChange(string owner, long operation, Vector2Int POS, bool front, bool success)
        {
            BlockChanges.Enqueue((owner, operation, POS, front, success));
        }

        public override void TechLateFrameUpdate()
        {
            SendBlockUpdate(); // common block updates (server / players)
            SendBlockChanges(); // unlocking client-side & telling the real block state
        }

        private void SendBlockUpdate()
        {
            Dictionary<string, Queue<(Vector2Int block, BlockData2 data)>> IndividualUpdates = new();

            foreach (string nickname in Ref<PlayerManager>().GetAllPlayerNicknames())
            {
                IndividualUpdates[nickname] = new();
            }

            while (BlockUpdates.Count > 0)
            {
                var element = BlockUpdates.Dequeue();
                Vector2Int chunk = BlockUtils.CoordsToChunk(element.block);

                foreach (string nickname in Ref<PlayerManager>().GetAllPlayerNicknames())
                {
                    if (Ref<PlayerManager>().PlayerHasChunk(nickname, chunk))
                        IndividualUpdates[nickname].Enqueue(element);
                }
            }

            foreach (string nickname in Ref<PlayerManager>().GetAllPlayerNicknames())
            {
                Queue<(Vector2Int block, BlockData2 data)> changes = IndividualUpdates[nickname];
                BlockUpdate.Record[] records = changes.Select(ch => new BlockUpdate.Record
                {
                    POS = ch.block,
                    Block = ch.data,
                }).ToArray();

                List<BlockUpdate> packets = BlockUpdate.CreateList(records);
                foreach (Payload packet in packets)
                {
                    Ref<QuickServer>().Send(nickname, packet);
                }
            }
        }

        private void SendBlockChanges()
        {
            while(BlockChanges.Count > 0)
            {
                var element = BlockChanges.Dequeue();

                string nickname = element.owner;
                Vector2Int POS = element.POS;
                Vector2Int chunk = BlockUtils.CoordsToChunk(POS);
                bool front = element.front;
                bool success = element.success;
                long operation = element.operation;

                BlockServer blockFront = WorldAPI.GetBlock(POS, true);
                BlockServer blockBack = WorldAPI.GetBlock(POS, false);

                if (
                    Ref<PlayerManager>().GetPlayerState(nickname) != Entities.PlayerManager.PlayerState.None &&
                    Ref<PlayerManager>().PlayerHasChunk(nickname, chunk) &&
                    blockFront != null && blockBack != null
                    )
                {
                    BlockData2 currentBlock = new BlockData2(blockFront.BlockData, blockBack.BlockData);

                    Payload packet = new RetBlockChange(POS, operation, currentBlock, front, success);
                    Ref<QuickServer>().Send(nickname, packet);
                }
            }
        }
    }
}
