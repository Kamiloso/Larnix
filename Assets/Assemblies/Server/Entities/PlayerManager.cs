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
    internal class PlayerManager : Singleton
    {
        private readonly Dictionary<string, ulong> _playerUIDs = new();
        private readonly Dictionary<string, PlayerUpdate> _recentPlayerUpdates = new(); // present for alive and dead players
        private readonly Dictionary<string, HashSet<ulong>> _nearbyUIDs = new();
        private readonly Dictionary<string, HashSet<Vec2Int>> _clientChunks = new();

        private Server Server => Ref<Server>();
        private QuickServer QuickServer => Ref<QuickServer>();
        private UserManager UserManager => Ref<UserManager>();
        private EntityManager EntityManager => Ref<EntityManager>();
        private Generator Worldgen => Ref<Generator>();

        public enum PlayerState : byte
        {
            None, // non-existent or disconnected
            Inactive, // spawned but not activated yet
            Alive, // alive and somewhere in the world
            Dead // player entity doesn't exist, but player is connected
        }

        public PlayerManager(Server server) : base(server) {}

        public void JoinPlayer(string nickname)
        {
            ulong uid = (ulong)UserManager.GetUserID(nickname);
            _playerUIDs[nickname] = uid;
            _nearbyUIDs[nickname] = new();
            _clientChunks[nickname] = new();

            CreatePlayerInstance(nickname);
        }

        public void CreatePlayerInstance(string nickname)
        {
            EntityManager.CreatePlayerController(nickname);
            
            // Set to PlayerState.Inactive
            if(_recentPlayerUpdates.ContainsKey(nickname))
            {
                _recentPlayerUpdates.Remove(nickname);
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
                _recentPlayerUpdates[nickname] = msg;
            }
        }

        public ulong UidByNickname(string nickname)
        {
            if (_playerUIDs.TryGetValue(nickname, out ulong uid))
                return uid;
            
            throw new KeyNotFoundException("Player " + nickname + " is not connected!");
        }

        public PlayerUpdate RecentPlayerUpdate(string nickname)
        {
            if (_recentPlayerUpdates.TryGetValue(nickname, out PlayerUpdate lastPacket))
                return lastPacket;
            
            return null;
        }

        public void DisconnectPlayer(string nickname)
        {
            if(EntityManager.GetPlayerController(nickname) != null)
                EntityManager.UnloadPlayerController(nickname);

            _playerUIDs.Remove(nickname);

            if(_recentPlayerUpdates.ContainsKey(nickname))
                _recentPlayerUpdates.Remove(nickname);

            _nearbyUIDs.Remove(nickname);
            _clientChunks.Remove(nickname);
        }

        public void UpdateNearbyUIDs(string nickname, HashSet<ulong> newUIDs, uint fixedFrame, bool sendAtLeastOne)
        {
            HashSet<ulong> oldUIDs = _nearbyUIDs[nickname];

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

            _nearbyUIDs[nickname] = newUIDs;
        }

        public void SendFrameInfoBroadcast()
        {
            foreach (string nickname in _playerUIDs.Keys)
            {
                Vec2 renderingPosition = RenderingPosition(nickname);

                Payload packet = new FrameInfo(
                    Server.ServerTick,
                    Worldgen.SkyColorAt(renderingPosition),
                    Worldgen.BiomeAt(renderingPosition),
                    Weather.Clear, // TODO: implement weather
                    Server.TPS
                );
                QuickServer.Send(nickname, packet, false);
            }
        }

        public bool PlayerHasChunk(string nickname, Vec2Int chunk)
        {
            if (_clientChunks.TryGetValue(nickname, out var chunks))
                return chunks.Contains(chunk);
            
            return false;
        }

        public void UpdateClientChunks(string nickname, HashSet<Vec2Int> chunks)
        {
            if (_clientChunks.ContainsKey(nickname))
                _clientChunks[nickname] = chunks;
        }

        public HashSet<Vec2Int> LoadedChunksCopy(string nickname)
        {
            if (!_clientChunks.ContainsKey(nickname))
                return new HashSet<Vec2Int>();

            return new HashSet<Vec2Int>(_clientChunks[nickname]);
        }

        public IEnumerable<string> AllPlayers() => _playerUIDs.Keys;
        public IEnumerable<string> AllPlayersThatAre(PlayerState state)
        {
            foreach (string nickname in _playerUIDs.Keys)
            {
                if (StateOf(nickname) == state)
                {
                    yield return nickname;
                }
            }
        }

        public IEnumerable<string> AllPlayersInRange(Vec2 position, double range)
        {
            foreach (string nickname in _playerUIDs.Keys)
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
                    return _recentPlayerUpdates[nickname].Position;

                default:
                    throw new InvalidOperationException("Player " + nickname + " is not connected!");
            }
        }

        public PlayerState StateOf(string nickname)
        {
            if (!_playerUIDs.ContainsKey(nickname))
                return PlayerState.None;

            if (!_recentPlayerUpdates.ContainsKey(nickname))
                return PlayerState.Inactive;

            if (EntityManager.GetPlayerController(nickname) != null)
                return PlayerState.Alive;

            return PlayerState.Dead;
        }
    }
}
