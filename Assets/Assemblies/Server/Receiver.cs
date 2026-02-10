using System.Collections;
using System.Collections.Generic;
using Larnix.Packets;
using Larnix.Server.Entities;
using Larnix.Server.Terrain;
using Larnix.Core.Utils;
using Larnix.Socket.Backend;
using Larnix.Socket.Packets.Control;
using Larnix.Core.Vectors;
using System;
using Larnix.Socket.Packets;

namespace Larnix.Server
{
    internal class Receiver : Singleton
    {
        private record RateLimitID(string Owner, Type Type);
        private readonly Dictionary<RateLimitID, int> _rateLimits = new();
        private float _rateLimitTimer = 0f;
        
        private readonly HashSet<string> _blackList = new();

        private WorldAPI WorldAPI => Ref<WorldAPI>();
        private QuickServer QuickServer => Ref<QuickServer>();
        private PlayerManager PlayerManager => Ref<PlayerManager>();
        private BlockSender BlockSender => Ref<BlockSender>();

        public Receiver(Server server) : base(server)
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

        public void Tick(float deltaTime)
        {
            _rateLimitTimer += deltaTime;
            if (_rateLimitTimer >= 1f)
            {
                _rateLimitTimer %= 1f;
                _rateLimits.Clear();
            }
        }

        private void Subscribe<T>(Action<T, string> callback, int maxPerSecond = 0, bool softLimit = false) where T : Payload, new()
        {
            QuickServer.Subscribe<T>((msg, owner) =>
            {
                if (typeof(T) != typeof(Stop) && _blackList.Contains(owner))
                    return; // discard packets from blacklisted clients

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
                            QuickServer.RequestFinishConnection(owner);
                            _blackList.Add(owner);
                        }
                    }
                }
                else // no rate limit
                {
                    callback(msg, owner);
                }
            });
        }

        private void _AllowConnection(AllowConnection msg, string owner)
        {
            PlayerManager.JoinPlayer(owner);
            Core.Debug.Log(owner + " joined the game.");
        }

        private void _Stop(Stop msg, string owner)
        {
            _blackList.Remove(owner);
            PlayerManager.DisconnectPlayer(owner);
            Core.Debug.Log(owner + " disconnected.");
        }

        private void _PlayerUpdate(PlayerUpdate msg, string owner)
        {
            // check if most recent data (fast mode receiving = over raw udp)
            PlayerUpdate lastPacket = PlayerManager.GetRecentPlayerUpdate(owner);
            if (lastPacket == null || lastPacket.FixedFrame < msg.FixedFrame)
            {
                PlayerManager.UpdatePlayerDataIfHasController(owner, msg);
            }
        }

        private void _CodeInfo(CodeInfo msg, string owner)
        {
            CodeInfo.Info code = msg.Code;

            if (code == CodeInfo.Info.RespawnMe)
            {
                if (PlayerManager.GetPlayerState(owner) == PlayerManager.PlayerState.Dead)
                    PlayerManager.CreatePlayerInstance(owner);
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
                bool has_chunk = PlayerManager.PlayerHasChunk(owner, chunk);
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
                bool has_chunk = PlayerManager.PlayerHasChunk(owner, chunk);
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
