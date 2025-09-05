using System.Collections;
using System.Collections.Generic;
using Larnix.Packets;
using QuickNet.Channel.Cmds;

namespace Larnix.Client
{
    public class Receiver
    {
        public Receiver(QuickNet.Frontend.QuickClient client)
        {
            client.Subscribe<PlayerInitialize>(_PlayerInitialize);
            client.Subscribe<EntityBroadcast>(_EntityBroadcast);
            client.Subscribe<NearbyEntities>(_NearbyEntities);
            client.Subscribe<CodeInfo>(_CodeInfo);
            client.Subscribe<ChunkInfo>(_ChunkInfo);
            client.Subscribe<BlockUpdate>(_BlockUpdate);
            client.Subscribe<RetBlockChange>(_RetBlockChange);
        }

        private void _PlayerInitialize(PlayerInitialize msg)
        {
            Ref.MainPlayer.LoadPlayerData(msg);
            Ref.Client.MyUID = msg.MyUid;
            Ref.Loading.StartWaitingFrom(msg.LastFixedFrame);
        }

        private void _EntityBroadcast(EntityBroadcast msg)
        {
            Ref.EntityProjections.InterpretEntityBroadcast(msg);
        }

        private void _NearbyEntities(NearbyEntities msg)
        {
            Ref.EntityProjections.ChangeNearbyUIDs(msg);
        }

        private void _CodeInfo(CodeInfo msg)
        {
            switch (msg.Code)
            {
                case CodeInfo.Info.YouDie: Ref.MainPlayer.SetDead(); break;
                default: break;
            }
        }

        private void _ChunkInfo(ChunkInfo msg)
        {
            if (msg.Blocks != null) // activation packet
            {
                Ref.GridManager.AddChunk(msg.Chunkpos, msg.Blocks);
            }
            else // removal packet
            {
                Ref.GridManager.RemoveChunk(msg.Chunkpos);
            }
        }

        private void _BlockUpdate(BlockUpdate msg)
        {
            BlockUpdate.Record[] records = msg.BlockUpdates;
            foreach (var rec in records)
            {
                Ref.GridManager.UpdateBlock(rec.POS, rec.Block);
            }
        }

        private void _RetBlockChange(RetBlockChange msg)
        {
            Ref.GridManager.UpdateBlock(
                msg.BlockPosition,
                msg.CurrentBlock,
                msg.Operation
            ); // unlock and update block
        }
    }
}
