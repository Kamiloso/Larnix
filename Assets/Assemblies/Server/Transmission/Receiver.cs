using System.Collections;
using System.Collections.Generic;
using Larnix.Packets;
using Larnix.Server.Entities;
using Larnix.Core.Utils;
using Larnix.Socket.Backend;
using Larnix.Socket.Packets.Control;
using Larnix.Core.Vectors;
using System;
using Larnix.Socket.Packets;
using Larnix.Blocks;
using Larnix.Core;

namespace Larnix.Server.Transmission
{
    internal class Receiver
    {
        private record RateLimitID(string Owner, Type Type);
        private readonly Dictionary<RateLimitID, int> _rateLimits = new();
        private float _rateLimitTimer = 0f;

        private IWorldAPI WorldAPI => GlobRef.Get<IWorldAPI>();
        private QuickServer QuickServer => GlobRef.Get<QuickServer>();
        private PlayerActions PlayerActions => GlobRef.Get<PlayerActions>();
        private BlockSender BlockSender => GlobRef.Get<BlockSender>();

        public Receiver()
        {
            Subscribe<AllowConnection>(_AllowConnection); // START (server validated)
            Subscribe<Stop>(_Stop); // STOP (server generated)

            // Assumptions:
            // - limit packets to ~2x expected max rate
            // - make soft limit when packet is not necessary for game integrity

            Subscribe<PlayerUpdate>(_PlayerUpdate, maxPerSecond: 100, softLimit: true);
            Subscribe<CodeInfo>(_CodeInfo, maxPerSecond: 10);
            Subscribe<BlockChange>(_BlockChange, maxPerSecond: 500); // TODO: Limit for survival players
        }

        private void Subscribe<T>(Action<T, string> callback, int maxPerSecond = 0,
            bool softLimit = false) where T : Payload
        {
            QuickServer.Subscribe<T>((msg, owner) =>
            {
                if (typeof(T) != typeof(Stop) && !QuickServer.IsActiveConnection(owner))
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
                            Core.Debug.Log("Rate limit for packet " + typeof(T).Name + " from " + owner + " exceeded.");
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

            PlayerActions.JoinPlayer(owner);
            Core.Debug.Log(owner + " joined the game.");
        }

        private void _Stop(Stop msg, string owner)
        {
            // WARNING:
            // Stop executes in a non-synchronized player context.
            // No player data methods are reliable here.

            PlayerActions.DisconnectPlayer(owner);
            Core.Debug.Log(owner + " disconnected.");
        }

        private void _PlayerUpdate(PlayerUpdate msg, string owner)
        {
            PlayerUpdate lastPacket = PlayerActions.RecentPlayerUpdate(owner);
            if (lastPacket == null || lastPacket.FixedFrame < msg.FixedFrame)
            {
                PlayerActions.UpdatePlayerDataIfHasController(owner, msg);
            }
        }

        private void _CodeInfo(CodeInfo msg, string owner)
        {
            CodeInfo.Info code = msg.Code;

            if (code == CodeInfo.Info.RespawnMe)
            {
                if (PlayerActions.StateOf(owner) == PlayerActions.PlayerState.Dead)
                    PlayerActions.CreatePlayerInstance(owner);
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
                bool has_item = true;
                bool has_chunk = PlayerActions.PlayerHasChunk(owner, chunk);
                bool can_place = WorldAPI.CanPlaceBlock(POS, front, msg.Item);

                bool success = has_item && has_chunk && can_place;

                if (success)
                {
                    WorldAPI.PlaceBlockWithEffects(POS, front, msg.Item);
                }

                BlockSender.AddRetBlockChange(new BlockChangeItem(owner, msg.Operation, POS, front, success));
            }

            else if (code == 1) // break using item
            {
                bool has_tool = true;
                bool has_chunk = PlayerActions.PlayerHasChunk(owner, chunk);
                bool can_break = WorldAPI.CanBreakBlock(POS, front, msg.Item, msg.Tool);

                bool success = has_tool && has_chunk && can_break;

                if (success)
                {
                    WorldAPI.BreakBlockWithEffects(POS, front, msg.Tool);
                }

                BlockSender.AddRetBlockChange(new BlockChangeItem(owner, msg.Operation, POS, front, success));
            }
        }
    }
}
