using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Larnix.Entities;
using Socket.Channel;
using Larnix.Packets;
using Larnix.Core.Vectors;

namespace Larnix.Server.Entities
{
    internal class PlayerManager
    {
        public readonly Dictionary<string, ulong> PlayerUID = new();

        public readonly Dictionary<string, PlayerUpdate> RecentPlayerUpdates = new(); // valid even for dead players, invalid for inactive players
        public readonly Dictionary<string, HashSet<ulong>> NearbyUIDs = new();
        public readonly Dictionary<string, HashSet<Vector2Int>> ClientChunks = new();

        public enum PlayerState : byte
        {
            None, // non-existent or disconnected
            Inactive, // spawned but not activated yet
            Alive, // alive and somewhere in the world
            Dead // player entity doesn't exist, but player is connected
        }

        public void JoinPlayer(string nickname)
        {
            ulong uid = (ulong)Ref.QuickServer.UserManager.GetUserID(nickname);
            PlayerUID[nickname] = uid;
            NearbyUIDs[nickname] = new();
            ClientChunks[nickname] = new();

            CreatePlayerInstance(nickname);
        }

        public void CreatePlayerInstance(string nickname)
        {
            Ref.EntityManager.CreatePlayerController(nickname);
            
            // Set to PlayerState.Inactive
            if(RecentPlayerUpdates.ContainsKey(nickname))
                RecentPlayerUpdates.Remove(nickname);
        }

        public void UpdatePlayerDataIfHasController(string nickname, PlayerUpdate msg)
        {
            EntityAbstraction playerController = Ref.EntityManager.GetPlayerController(nickname);
            if (playerController != null) // Player is either PlayerState.Inactive or PlayerState.Alive
            {
                // Activate controller if not active
                if (!playerController.IsActive)
                    playerController.Activate();

                // Load data to player controller
                ((Player)playerController.GetRealController()).UpdateTransform(
                    msg.Position, msg.Rotation);

                // Update PlayerUpdate info
                RecentPlayerUpdates[nickname] = msg;
            }
        }

        public void DisconnectPlayer(string nickname)
        {
            if(Ref.EntityManager.GetPlayerController(nickname) != null)
                Ref.EntityManager.UnloadPlayerController(nickname);

            PlayerUID.Remove(nickname);

            if(RecentPlayerUpdates.ContainsKey(nickname))
                RecentPlayerUpdates.Remove(nickname);

            NearbyUIDs.Remove(nickname);
            ClientChunks.Remove(nickname);
        }

        public void UpdateNearbyUIDs(string nickname, HashSet<ulong> newUIDs, uint fixedFrame, bool sendAtLeastOne)
        {
            HashSet<ulong> oldUIDs = NearbyUIDs[nickname];

            HashSet<ulong> added = new HashSet<ulong>(newUIDs);
            added.ExceptWith(oldUIDs);

            HashSet<ulong> removed = new HashSet<ulong>(oldUIDs);
            removed.ExceptWith(newUIDs);

            ulong[] addedList = added.ToArray();
            ulong[] removedList = removed.ToArray();

            List<NearbyEntities> packets = NearbyEntities.CreateList(fixedFrame, addedList, removedList);
            if (packets.Count > 0)
            {
                foreach(Packet packet in packets)
                {
                    Ref.QuickServer.Send(nickname, packet);
                }
            }
            else
            {
                if (sendAtLeastOne)
                {
                    Ref.QuickServer.Send(nickname, new NearbyEntities(fixedFrame, null, null));
                }
            }

            NearbyUIDs[nickname] = newUIDs;
        }

        public bool PlayerHasChunk(string nickname, Vector2Int chunk)
        {
            return ClientChunks.ContainsKey(nickname);
        }

        public PlayerState GetPlayerState(string nickname)
        {
            if (!PlayerUID.ContainsKey(nickname))
                return PlayerState.None;

            if (!RecentPlayerUpdates.ContainsKey(nickname))
                return PlayerState.Inactive;

            if (Ref.EntityManager.GetPlayerController(nickname) != null)
                return PlayerState.Alive;

            return PlayerState.Dead;
        }

        public HashSet<string> GetAllPlayersThatAre(PlayerState state)
        {
            HashSet<string> result = new();
            foreach (string nickname in PlayerUID.Keys)
            {
                if (GetPlayerState(nickname) == state)
                    result.Add(nickname);
            }
            return result;
        }

        public Vec2 GetPlayerRenderingPosition(string nickname)
        {
            PlayerState state = GetPlayerState(nickname);

            switch(state)
            {
                case PlayerState.Inactive:
                case PlayerState.Alive:
                    return Ref.EntityManager.GetPlayerController(nickname).EntityData.Position;

                case PlayerState.Dead:
                    return RecentPlayerUpdates[nickname].Position;

                default:
                    throw new System.InvalidOperationException("Player " + nickname + " is not connected!");
            }
        }

        public Dictionary<ulong, uint> GetFixedFramesByUID()
        {
            Dictionary<ulong, uint> returns = new();
            foreach(string nickname in PlayerUID.Keys)
            {
                if(RecentPlayerUpdates.ContainsKey(nickname))
                    returns[PlayerUID[nickname]] = RecentPlayerUpdates[nickname].FixedFrame;
            }
            return returns;
        }
    }
}
