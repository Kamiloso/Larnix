using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Socket.Frontend;
using Larnix.Packets;
using Larnix.Packets.Structs;
using Larnix.Client.Entities;
using Larnix.Client.Terrain;
using Larnix.Client.UI;
using Larnix.Client.Particles;
using UnityEngine;
using Larnix.Blocks;
using Larnix.Background;

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
        private Sky Sky => Ref.Sky;
        private Debugger Debugger => Ref.Debugger;

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
            client.Subscribe<FrameInfo>(_FrameInfo);
        }

        private void _PlayerInitialize(PlayerInitialize msg)
        {
            MainPlayer.LoadPlayerData(msg);
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
                GridManager.UpdateBlock(rec.Position, rec.Block, rec.BreakMode);
            }
        }

        private void _RetBlockChange(RetBlockChange msg)
        {
            GridManager.UpdateBlock(
                POS: msg.BlockPosition,
                data: msg.CurrentBlock,
                breakMode: IWorldAPI.BreakMode.Replace,
                unlock: msg.Operation
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

        private long _lastFrameTick = 0;
        private void _FrameInfo(FrameInfo msg)
        {
            long frameTick = msg.ServerTick;
            if (frameTick > _lastFrameTick)
            {
                _lastFrameTick = frameTick;

                Sky.UpdateSky(
                    biomeID: msg.BiomeID,
                    skyColor: msg.SkyColor,
                    weather: msg.Weather
                    );

                Debugger.ServerTick = frameTick;
                Debugger.CurrentBiome = msg.BiomeID;
            }
        }
    }
}
