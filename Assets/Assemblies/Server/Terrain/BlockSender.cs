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

    internal class BlockSender : Singleton
    {
        private readonly Queue<BlockUpdateRecord> _blockUpdates = new();
        private readonly Queue<BlockChangeItem> _blockChanges = new();

        private WorldAPI WorldAPI => Ref<WorldAPI>();
        private PlayerManager PlayerManager => Ref<PlayerManager>();
        private QuickServer QuickServer => Ref<QuickServer>();

        public BlockSender(Server server) : base(server) {}

        public void AddBlockUpdate(BlockUpdateRecord element)
        {
            _blockUpdates.Enqueue(element);
        }

        public void AddRetBlockChange(BlockChangeItem element)
        {
            _blockChanges.Enqueue(element);
        }

        public override void PostLateFrameUpdate()
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
    }
}
