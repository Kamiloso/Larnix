using Larnix.Core.Vectors;
using Larnix.Model.Packets;
using Larnix.Server.Packets.Structs;
using Larnix.Client.Entities;
using Larnix.Client.Terrain;
using Larnix.Client.UI;
using Larnix.Client.Particles;
using Larnix.Core;
using Larnix.Background;
using Larnix.Client.Chat;
using Larnix.Model.Blocks;
using Larnix.Socket.Client;
using Larnix.Model.Blocks.Chunks;

namespace Larnix.Client;

public class Receiver
{
    private Loading Loading => GlobRef.Get<Loading>();
    private MainPlayer MainPlayer => GlobRef.Get<MainPlayer>();
    private GridManager GridManager => GlobRef.Get<GridManager>();
    private EntityProjections EntityProjections => GlobRef.Get<EntityProjections>();
    private ParticleManager ParticleManager => GlobRef.Get<ParticleManager>();
    private ChatManager ChatManager => GlobRef.Get<ChatManager>();
    private Sky Sky => GlobRef.Get<Sky>();
    private Debugger Debugger => GlobRef.Get<Debugger>();

    public Receiver(QuickClient client)
    {
        client.OnReceive<PlayerInitialize>(_PlayerInitialize);
        client.OnReceive<EntityBroadcast>(_EntityBroadcast);
        client.OnReceive<NearbyEntities>(_NearbyEntities);
        client.OnReceive<CodeInfo>(_CodeInfo);
        client.OnReceive<ChunkInfo>(_ChunkInfo);
        client.OnReceive<BlockUpdate>(_BlockUpdate);
        client.OnReceive<RetBlockChange>(_RetBlockChange);
        client.OnReceive<Teleport>(_Teleport);
        client.OnReceive<SpawnParticles>(_SpawnParticles);
        client.OnReceive<FrameInfo>(_FrameInfo);
        client.OnReceive<ChatMessage>(_ChatMessage);
    }

    private void _PlayerInitialize(in PlayerInitialize msg)
    {
        MainPlayer.LoadPlayerData(msg.Position, msg.Uid);
        Loading.StartWaitingFrom(msg.LastFixedFrame);
    }

    private void _EntityBroadcast(in EntityBroadcast msg)
    {
        EntityProjections.InterpretEntityBroadcast(msg);
    }

    private void _NearbyEntities(in NearbyEntities msg)
    {
        EntityProjections.ChangeNearbyUIDs(msg);
    }

    private void _CodeInfo(in CodeInfo msg)
    {
        switch (msg.Code)
        {
            case CodeInfo.Info.YouDie:
                MainPlayer.Alive = false;
                break;
        }
    }

    private void _ChunkInfo(in ChunkInfo msg)
    {
        if (msg.Chunk != null) // activation packet
        {
            ChunkData chunkData = new(msg.ChunkBytes());
            GridManager.AddChunk(msg.Chunk, chunkData);
        }
        else // removal packet
        {
            GridManager.RemoveChunk(msg.Chunk);
        }
    }

    private void _BlockUpdate(in BlockUpdate msg)
    {
        BlockUpdateRecord[] records = msg.BlockRecords.ToArray();
        foreach (var rec in records)
        {
            GridManager.UpdateBlock(rec.Position, rec.Block, rec.BreakMode);
        }
    }

    private void _RetBlockChange(in RetBlockChange msg)
    {
        GridManager.UpdateBlock(
            POS: msg.POS,
            data: msg.CurrentBlock,
            breakMode: IWorldAPI.BreakMode.Replace,
            unlock: msg.Operation
        ); // unlock and update block
    }

    private void _Teleport(in Teleport msg)
    {
        if (MainPlayer.Alive)
        {
            Vec2 targetPos = msg.Position;
            MainPlayer.Teleport(targetPos);
            Echo.Log("Teleported");
        }
    }

    private void _SpawnParticles(in SpawnParticles msg)
    {
        if (msg.IsEntityParticle())
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
    private void _FrameInfo(in FrameInfo msg)
    {
        long frameTick = msg.ServerTick;
        if (frameTick > _lastFrameTick)
        {
            _lastFrameTick = frameTick;

            Sky.UpdateSky(
                biomeID: msg.BiomeId,
                skyColor: msg.SkyColor,
                weather: msg.WeatherId
                );

            Debugger.InfoUpdate(
                serverTick: msg.ServerTick,
                biomeID: msg.BiomeId,
                tps: msg.Tps
            );
        }
    }

    private void _ChatMessage(in ChatMessage msg)
    {
        ChatManager.AddMessage(msg);
    }
}
