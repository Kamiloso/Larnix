using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Larnix.Entities.All;
using Larnix.Socket.Packets;
using Larnix.Core.Vectors;
using Larnix.Socket.Backend;
using Larnix.Packets;
using System;
using Larnix.Worldgen;
using Larnix.Core;

namespace Larnix.Server.Entities
{
    internal class PlayerActions : IScript
    {
        // internal representation aggregating all per-player state
        private class ConnectedPlayer
        {
            public ulong Uid;
            public PlayerUpdate RecentUpdate; // null while inactive
            public HashSet<ulong> NearbyUIDs = new();
            public HashSet<Vec2Int> ClientChunks = new();
        }

        private readonly Dictionary<string, ConnectedPlayer> _players = new();

        private Clock Clock => GlobRef.Get<Clock>();
        private Config Config => GlobRef.Get<Config>();
        private QuickServer QuickServer => GlobRef.Get<QuickServer>();
        private UserManager UserManager => GlobRef.Get<UserManager>();
        private EntityManager EntityManager => GlobRef.Get<EntityManager>();
        private Generator Worldgen => GlobRef.Get<Generator>();

        public enum PlayerState : byte
        {
            None, // non-existent or disconnected
            Inactive, // spawned but not activated yet
            Alive, // alive and somewhere in the world
            Dead // player entity doesn't exist, but player is connected
        }

        public void JoinPlayer(string nickname)
        {
            ulong uid = (ulong)UserManager.GetUserID(nickname);
            var cp = new ConnectedPlayer { Uid = uid };
            _players[nickname] = cp;

            CreatePlayerInstance(nickname);
        }

        public void CreatePlayerInstance(string nickname)
        {
            EntityManager.CreatePlayerController(nickname);
            
            // Set to PlayerState.Inactive by clearing any previous update
            if (_players.TryGetValue(nickname, out var cp))
            {
                cp.RecentUpdate = null;
            }
        }

        public void UpdatePlayerDataIfHasController(string nickname, PlayerUpdate msg)
        {
            EntityAbstraction playerController = EntityManager.GetPlayerController(nickname);
            if (playerController != null) // Player is either PlayerState.Inactive or PlayerState.Alive
            {
                // Activate controller if not active
                if (!playerController.IsActive)
                    playerController.Activate();

                // Load data to player controller
                ((Player)playerController.Controller).UpdateTransform(
                    msg.Position, msg.Rotation);

                // Update PlayerUpdate info
                if (_players.TryGetValue(nickname, out var cp))
                {
                    cp.RecentUpdate = msg;
                }
            }
        }

        public ulong UidByNickname(string nickname)
        {
            if (_players.TryGetValue(nickname, out var cp))
                return cp.Uid;
            
            throw new KeyNotFoundException("Player " + nickname + " is not connected!");
        }

        public PlayerUpdate RecentPlayerUpdate(string nickname)
        {
            if (_players.TryGetValue(nickname, out var cp))
                return cp.RecentUpdate;
            
            return null;
        }

        public void DisconnectPlayer(string nickname)
        {
            if(EntityManager.GetPlayerController(nickname) != null)
                EntityManager.UnloadPlayerController(nickname);

            _players.Remove(nickname);
        }

        public void UpdateNearbyUIDs(string nickname, HashSet<ulong> newUIDs, uint fixedFrame, bool sendAtLeastOne)
        {
            if (!_players.TryGetValue(nickname, out var cp))
                return;

            HashSet<ulong> oldUIDs = cp.NearbyUIDs;

            HashSet<ulong> added = new HashSet<ulong>(newUIDs);
            added.ExceptWith(oldUIDs);

            HashSet<ulong> removed = new HashSet<ulong>(oldUIDs);
            removed.ExceptWith(newUIDs);

            ulong[] addedList = added.ToArray();
            ulong[] removedList = removed.ToArray();

            List<NearbyEntities> packets = NearbyEntities.CreateList(fixedFrame, addedList, removedList);
            if (packets.Count > 0)
            {
                foreach(Payload packet in packets)
                {
                    QuickServer.Send(nickname, packet);
                }
            }
            else
            {
                if (sendAtLeastOne)
                {
                    QuickServer.Send(nickname, NearbyEntities.CreateBootstrap(fixedFrame));
                }
            }

            cp.NearbyUIDs = newUIDs;
        }

        void IScript.PostLateFrameUpdate()
        {
            if (Clock.FixedFrame % Config.EntityBroadcastPeriodFrames == 0)
            {
                foreach (string nickname in _players.Keys)
                {
                    Vec2 renderingPosition = RenderingPosition(nickname);

                    Payload packet = new FrameInfo(
                        Clock.ServerTick,
                        Worldgen.SkyColorAt(renderingPosition),
                        Worldgen.BiomeAt(renderingPosition),
                        Weather.Clear, // TODO: implement weather
                        Clock.TPS
                    );
                    QuickServer.Send(nickname, packet, false);
                }
            }
        }

        public bool PlayerHasChunk(string nickname, Vec2Int chunk)
        {
            if (_players.TryGetValue(nickname, out var cp))
                return cp.ClientChunks.Contains(chunk);
            
            return false;
        }

        public void UpdateClientChunks(string nickname, HashSet<Vec2Int> chunks)
        {
            if (_players.TryGetValue(nickname, out var cp))
                cp.ClientChunks = chunks;
        }

        public HashSet<Vec2Int> LoadedChunksCopy(string nickname)
        {
            if (!_players.TryGetValue(nickname, out var cp))
                return new HashSet<Vec2Int>();

            return new HashSet<Vec2Int>(cp.ClientChunks);
        }

        public IEnumerable<string> AllPlayers() => _players.Keys;
        public IEnumerable<string> AllPlayersThatAre(PlayerState state)
        {
            foreach (string nickname in _players.Keys)
            {
                if (StateOf(nickname) == state)
                {
                    yield return nickname;
                }
            }
        }

        public IEnumerable<string> AllPlayersInRange(Vec2 position, double range)
        {
            foreach (string nickname in _players.Keys)
            {
                Vec2 playerPos = RenderingPosition(nickname);
                if (Vec2.Distance(playerPos, position) <= range)
                {
                    yield return nickname;
                }
            }
        }

        public Vec2 RenderingPosition(string nickname)
        {
            PlayerState state = StateOf(nickname);
            switch(state)
            {
                case PlayerState.Inactive:
                case PlayerState.Alive:
                    return EntityManager.GetPlayerController(nickname).ActiveData.Position;

                case PlayerState.Dead:
                    return _players[nickname].RecentUpdate.Position;

                default:
                    throw new InvalidOperationException("Player " + nickname + " is not connected!");
            }
        }

        public PlayerState StateOf(string nickname)
        {
            if (!_players.ContainsKey(nickname))
                return PlayerState.None;

            var cp = _players[nickname];
            if (cp.RecentUpdate == null)
                return PlayerState.Inactive;

            if (EntityManager.GetPlayerController(nickname) != null)
                return PlayerState.Alive;

            return PlayerState.Dead;
        }

        public bool IsConnected(string nickname) => StateOf(nickname) != PlayerState.None;
        public bool IsAlive(string nickname) => StateOf(nickname) == PlayerState.Alive;
    }
}
