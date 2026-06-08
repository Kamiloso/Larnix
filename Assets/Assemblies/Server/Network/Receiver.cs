#nullable enable
using Larnix.Core;
using Larnix.Core.Vectors;
using Larnix.Model.Blocks;
using Larnix.Server.Chunks.Scripts;
using Larnix.Server.Commands;
using Larnix.Server.Entities;
using Larnix.Model.Packets;
using System;
using System.Collections.Generic;
using System.Net;

namespace Larnix.Server.Network;

internal class Receiver
{
    private record RateLimitID(string Owner, Type Type);
    private readonly Dictionary<RateLimitID, int> _rateLimits = new();
    private float _rateLimitTimer = 0f;

    private IWorldAPI WorldAPI => GlobRef.Get<IWorldAPI>();
    private IConnectedPlayers ConnectedPlayers => GlobRef.Get<IConnectedPlayers>();
    private IChunkSender ChunkSender => GlobRef.Get<IChunkSender>();
    private IChat Chat => GlobRef.Get<IChat>();

    private readonly IServer _server;

    public Receiver(IServer server)
    {
        _server = server;

        _server.OnConnected(__Start);
        _server.OnDisconnected(__Stop);

        // Assumptions:
        // - limit packets to ~4x expected max rate
        // - make soft limit when packet is not necessary for game integrity

        Subscribe<PlayerUpdate>(_PlayerUpdate, maxPerSecond: 200, softLimit: true);
        Subscribe<CodeInfo>(_CodeInfo, maxPerSecond: 20);
        Subscribe<BlockChange>(_BlockChange, maxPerSecond: 1000); // TODO: Limit for survival players
        Subscribe<ChatMessage>(_ChatMessage, maxPerSecond: 20, softLimit: true);
    }

    private void Subscribe<T>(Action<T, string> callback, int maxPerSecond = 0,
        bool softLimit = false) where T : unmanaged
    {
        _server.OnReceive((in T msg, string owner) =>
        {
            if (maxPerSecond > 0) // rate limit
            {
                var id = new RateLimitID(owner, typeof(T));
                int current = _rateLimits.GetValueOrDefault(id, 0);

                if (current < maxPerSecond)
                {
                    _rateLimits[id] = current + 1;
                    callback(msg, owner);
                }
                else
                {
                    if (!softLimit) // hard limit - disconnect client
                    {
                        Echo.Log($"Rate limit for packet {typeof(T).Name} from {owner} exceeded.");
                        _server.KickRequest(owner);
                    }
                }
            }
            else // no rate limit
            {
                callback(msg, owner);
            }
        });
    }

    public void Tick(float deltaTime)
    {
        _rateLimitTimer += deltaTime;
        if (_rateLimitTimer >= 1f)
        {
            _rateLimitTimer %= 1f;
            _rateLimits.Clear();
        }
    }

    private void __Start(string owner, IPEndPoint endpoint)
    {
        ConnectedPlayers.JoinPlayer(owner, endpoint);
        Echo.Log($"{owner} joined the game.");
    }

    private void __Stop(string owner)
    {
        ConnectedPlayers.DisconnectPlayer(owner);
        Echo.Log($"{owner} disconnected.");
    }

    private void _PlayerUpdate(PlayerUpdate msg, string owner)
    {
        JoinedPlayer player = ConnectedPlayers[owner];
        if (!player.HasPlayerUpdate || msg.FixedFrame > player.FixedFrame)
        {
            ConnectedPlayers.UpdatePlayer(owner, msg);
        }
    }

    private void _CodeInfo(CodeInfo msg, string owner)
    {
        switch (msg.Code)
        {
            case CodeInfo.Info.RespawnMe:
                if (ConnectedPlayers.StateOf(owner) == PlayerState.Dead)
                {
                    ConnectedPlayers.RespawnPlayer(owner);
                }
                break;
        }
    }

    private void _BlockChange(BlockChange msg, string owner)
    {
        Vec2Int POS = msg.POS;
        Vec2Int chunk = BlockHelpers.CoordsToChunk(POS);
        bool front = msg.IsFront;
        bool place = msg.IsPlace;

        if (place) // place item
        {
            bool hasItem = true;
            bool hasChunk = ConnectedPlayers[owner].LoadedChunks.Contains(chunk);
            bool canPlace = WorldAPI.CanPlaceBlock(POS, front, new(msg.Item));

            bool success = hasItem && hasChunk && canPlace;

            if (success)
            {
                WorldAPI.PlaceBlockWithEffects(POS, front, new(msg.Item));
            }

            ChunkSender.AddRetBlockChange(new BlockChangeItem(owner, msg.Operation, POS, front, success));
        }

        else // break using item
        {
            bool hasTool = true;
            bool hasChunk = ConnectedPlayers[owner].LoadedChunks.Contains(chunk);
            bool canBreak = WorldAPI.CanBreakBlock(POS, front, new(msg.Item), new(msg.Tool));

            bool success = hasTool && hasChunk && canBreak;

            if (success)
            {
                WorldAPI.BreakBlockWithEffects(POS, front, new(msg.Tool));
            }

            ChunkSender.AddRetBlockChange(new BlockChangeItem(owner, msg.Operation, POS, front, success));
        }
    }

    private void _ChatMessage(ChatMessage msg, string owner)
    {
        Chat.OnArrive(owner, msg.Message);
    }
}
