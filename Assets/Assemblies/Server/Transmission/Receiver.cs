#nullable enable
using Larnix.Core;
using Larnix.Core.Vectors;
using Larnix.Model.Blocks;
using Larnix.Model.Utils;
using Larnix.Server.Chunks.Scripts;
using Larnix.Server.Commands;
using Larnix.Server.Entities;
using Larnix.Server.Packets;
using Larnix.Socket.Backend;
using Larnix.Socket.Packets;
using Larnix.Socket.Packets.Control;
using System;
using System.Collections.Generic;
using static Larnix.Server.Packets.CodeInfo;

namespace Larnix.Server.Transmission;

internal class Receiver
{
    private record RateLimitID(string Owner, Type Type);
    private readonly Dictionary<RateLimitID, int> _rateLimits = new();
    private readonly HashSet<string> _limitedBlacklist = new();
    private float _rateLimitTimer = 0f;

    private IWorldAPI WorldAPI => GlobRef.Get<IWorldAPI>();
    private QuickServer QuickServer => GlobRef.Get<QuickServer>();
    private IConnectedPlayers ConnectedPlayers => GlobRef.Get<IConnectedPlayers>();
    private IChunkSender ChunkSender => GlobRef.Get<IChunkSender>();
    private IChat Chat => GlobRef.Get<IChat>();

    public Receiver()
    {
        Subscribe<AllowConnection>(_AllowConnection); // START (server validated)
        Subscribe<Stop>(_Stop); // STOP (server generated)

        // Assumptions:
        // - limit packets to ~4x expected max rate
        // - make soft limit when packet is not necessary for game integrity

        Subscribe<PlayerUpdate>(_PlayerUpdate, maxPerSecond: 200, softLimit: true);
        Subscribe<CodeInfo>(_CodeInfo, maxPerSecond: 20);
        Subscribe<BlockChange>(_BlockChange, maxPerSecond: 1000); // TODO: Limit for survival players
        Subscribe<ChatMessage>(_ChatMessage, maxPerSecond: 20, softLimit: true);
    }

    private void Subscribe<T>(Action<T, string> callback, int maxPerSecond = 0,
        bool softLimit = false) where T : Payload_Legacy
    {
        QuickServer.Subscribe<T>((msg, owner) =>
        {
            if (typeof(T) != typeof(Stop) && _limitedBlacklist.Contains(owner))
                return; // discard packets from kicked clients

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
                        _limitedBlacklist.Add(owner);
                        QuickServer.KickRequest(owner);
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

    private void _AllowConnection(AllowConnection msg, string owner)
    {
        // WARNING:
        // AllowConnection executes in a non-synchronized player context.
        // No player data methods are reliable here.

        ConnectedPlayers.JoinPlayer(owner);
        Echo.Log($"{owner} joined the game.");
    }

    private void _Stop(Stop msg, string owner)
    {
        // WARNING:
        // Stop executes in a non-synchronized player context.
        // No player data methods are reliable here.

        ConnectedPlayers.DisconnectPlayer(owner);
        _limitedBlacklist.Remove(owner);
        Echo.Log($"{owner} disconnected.");
    }

    private void _PlayerUpdate(PlayerUpdate msg, string owner)
    {
        JoinedPlayer player = ConnectedPlayers[owner];
        if (player.LastUpdate is null || msg.FixedFrame > player.LastUpdate.FixedFrame)
        {
            ConnectedPlayers.UpdatePlayer(owner, msg);
        }
    }

    private void _CodeInfo(CodeInfo msg, string owner)
    {
        switch (msg.Code)
        {
            case Info.RespawnMe:
                if (ConnectedPlayers.StateOf(owner) == PlayerState.Dead)
                {
                    ConnectedPlayers.RespawnPlayer(owner);
                }
                break;
        }
    }

    private void _BlockChange(BlockChange msg, string owner)
    {
        Vec2Int POS = msg.BlockPosition;
        Vec2Int chunk = BlockUtils.CoordsToChunk(POS);
        bool front = msg.Front;
        byte code = msg.Code;

        if (code == 0) // place item
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

        else if (code == 1) // break using item
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
