using Larnix.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Blocks;
using Larnix.Socket;
using Larnix.Socket.Commands;
using System;
using Unity.VisualScripting;

namespace Larnix.Server.Terrain
{
    public class BlockSender : MonoBehaviour
    {
        private readonly Queue<(Vector2Int block, BlockData data)> BlockUpdates = new();
        private readonly Queue<(string owner, long operation, Vector2Int POS, bool front, bool success)> BlockChanges = new();

        private void Awake()
        {
            References.BlockSender = this;
        }

        public void AddBlockUpdate((Vector2Int, BlockData) element)
        {
            BlockUpdates.Enqueue(element);
        }

        public void AddRetBlockChange(string owner, long operation, Vector2Int POS, bool front, bool success)
        {
            BlockChanges.Enqueue((owner, operation, POS, front, success));
        }

        public void BroadcastInfo()
        {
            SendBlockUpdate(); // common block updates (server / players)
            SendBlockChanges(); // unlocking client-side & telling the real block state
        }

        private void SendBlockUpdate()
        {
            Dictionary<string, Queue<(Vector2Int block, BlockData data)>> IndividualUpdates = new();

            foreach (string nickname in References.PlayerManager.PlayerUID.Keys)
            {
                IndividualUpdates[nickname] = new();
            }

            while (BlockUpdates.Count > 0)
            {
                var element = BlockUpdates.Dequeue();
                Vector2Int chunk = ChunkMethods.CoordsToChunk(element.block);

                foreach (string nickname in References.PlayerManager.PlayerUID.Keys)
                {
                    if (References.PlayerManager.ClientChunks[nickname].Contains(chunk))
                        IndividualUpdates[nickname].Enqueue(element);
                }
            }

            foreach (string nickname in References.PlayerManager.PlayerUID.Keys)
            {
                Queue<(Vector2Int block, BlockData data)> changes = IndividualUpdates[nickname];

                BlockUpdate blockUpdate = new BlockUpdate(new List<(Vector2Int block, BlockData data)>());

                while (changes.Count > 0)
                {
                    var element = changes.Dequeue();
                    blockUpdate.BlockUpdates.Add(element);

                    if (blockUpdate.BlockUpdates.Count == BlockUpdate.MAX_RECORDS || changes.Count == 0)
                    {
                        Packet packet = blockUpdate.GetPacket();
                        References.Server.Send(nickname, packet);
                        blockUpdate.BlockUpdates.Clear();
                    }
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
                Vector2Int chunk = ChunkMethods.CoordsToChunk(POS);
                bool front = element.front;
                bool success = element.success;
                long operation = element.operation;

                BlockServer blockFront = WorldAPI.GetBlock(POS, true);
                BlockServer blockBack = WorldAPI.GetBlock(POS, false);

                if (
                    References.PlayerManager.GetPlayerState(nickname) != Entities.PlayerManager.PlayerState.None &&
                    References.PlayerManager.PlayerHasChunk(nickname, chunk) &&
                    blockFront != null && blockBack != null
                    )
                {
                    BlockData currentBlock = new BlockData(blockFront.BlockData, blockBack.BlockData);

                    RetBlockChange retBlockChange = new RetBlockChange(POS, operation, currentBlock, (byte)(front ? 1 : 0), (byte)(success ? 1 : 0));
                    if (!retBlockChange.HasProblems)
                    {
                        Packet packet = retBlockChange.GetPacket();
                        References.Server.Send(nickname, packet);
                    }
                    else throw new Exception("Failied constructing command RetBlockChange!");
                }
            }
        }
    }
}
