using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Socket.Frontend;
using Larnix.Packets;
using Larnix.Packets.Structs;
using UnityEngine;
using Larnix.Client.Relativity;
using Larnix.Client.Entities;
using Larnix.Client.Terrain;
using Larnix.Client.UI;

namespace Larnix.Client
{
    public class Receiver
    {
        private Client Client => Ref.Client;
        private Loading Loading => Ref.Loading;
        private MainPlayer MainPlayer => Ref.MainPlayer;
        private GridManager GridManager => Ref.GridManager;
        private EntityProjections EntityProjections => Ref.EntityProjections;
        private ParticleManager ParticleManager => Ref.ParticleManager;

        public Receiver(QuickClient client)
        {
            client.Subscribe<PlayerInitialize>(_PlayerInitialize);
            client.Subscribe<EntityBroadcast>(_EntityBroadcast);
            client.Subscribe<NearbyEntities>(_NearbyEntities);
            client.Subscribe<CodeInfo>(_CodeInfo);
            client.Subscribe<ChunkInfo>(_ChunkInfo);
            client.Subscribe<BlockUpdate>(_BlockUpdate);
            client.Subscribe<RetBlockChange>(_RetBlockChange);
            client.Subscribe<Teleport>(_Teleport);
            client.Subscribe<SpawnParticles>(_SpawnParticles);
        }

        private void _PlayerInitialize(PlayerInitialize msg)
        {
            MainPlayer.LoadPlayerData(msg);
            Client.MyUID = msg.MyUid;
            Loading.StartWaitingFrom(msg.LastFixedFrame);
        }

        private void _EntityBroadcast(EntityBroadcast msg)
        {
            EntityProjections.InterpretEntityBroadcast(msg);
        }

        private void _NearbyEntities(NearbyEntities msg)
        {
            EntityProjections.ChangeNearbyUIDs(msg);
        }

        private void _CodeInfo(CodeInfo msg)
        {
            switch (msg.Code)
            {
                case CodeInfo.Info.YouDie: MainPlayer.SetDead(); break;
                default: break;
            }
        }

        private void _ChunkInfo(ChunkInfo msg)
        {
            if (msg.Blocks != null) // activation packet
            {
                GridManager.AddChunk(msg.Chunkpos, msg.Blocks);
            }
            else // removal packet
            {
                GridManager.RemoveChunk(msg.Chunkpos);
            }
        }

        private void _BlockUpdate(BlockUpdate msg)
        {
            BlockUpdateRecord[] records = msg.BlockUpdates;
            foreach (var rec in records)
            {
                GridManager.UpdateBlock(rec.Position, rec.Block);
            }
        }

        private void _RetBlockChange(RetBlockChange msg)
        {
            GridManager.UpdateBlock(
                msg.BlockPosition,
                msg.CurrentBlock,
                msg.Operation
            ); // unlock and update block
        }

        private void _Teleport(Teleport msg)
        {
            if (MainPlayer.IsAlive)
            {
                Vec2 targetPos = msg.TargetPosition;
                MainPlayer.Teleport(targetPos);
                Core.Debug.Log("Teleported");
            }
        }

        private void _SpawnParticles(SpawnParticles msg)
        {
            if (msg.IsEntityParticle)
            {
                ParticleManager.SpawnEntityParticles(
                    msg.ParticleID,
                    msg.EntityUid,
                    msg.Position
                );
            }
            else
            {
                ParticleManager.SpawnGlobalParticles(
                    msg.ParticleID,
                    msg.Position
                );
            }
        }
    }
}
