using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QuickNet.Commands;
using System.Linq;
using QuickNet;
using Larnix.Entities;
using QuickNet.Channel;
using Larnix.Network;

namespace Larnix.Server.Entities
{
    public class PlayerManager : MonoBehaviour
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

        private void Awake()
        {
            References.PlayerManager = this;
        }

        public void JoinPlayer(string nickname)
        {
            ulong uid = (ulong)References.Server.LarnixServer.UserManager.GetUserID(nickname);
            PlayerUID[nickname] = uid;
            NearbyUIDs[nickname] = new();
            ClientChunks[nickname] = new();

            CreatePlayerInstance(nickname);
        }

        public void CreatePlayerInstance(string nickname)
        {
            References.EntityManager.CreatePlayerController(nickname);
            
            // Set to PlayerState.Inactive
            if(RecentPlayerUpdates.ContainsKey(nickname))
                RecentPlayerUpdates.Remove(nickname);
        }

        public void UpdatePlayerDataIfHasController(string nickname, PlayerUpdate msg)
        {
            EntityAbstraction playerController = References.EntityManager.GetPlayerController(nickname);
            if (playerController != null) // Player is either PlayerState.Inactive or PlayerState.Alive
            {
                // Activate controller if not active
                if (!playerController.IsActive)
                    playerController.Activate();

                // Load data to player controller
                EntityData entityData = playerController.EntityData.ShallowCopy();
                entityData.Position = msg.Position;
                entityData.Rotation = msg.Rotation;
                playerController.GetRealController().UpdateEntityData(entityData);

                // Update PlayerUpdate info
                RecentPlayerUpdates[nickname] = msg;
            }
        }

        public void DisconnectPlayer(string nickname)
        {
            if(References.EntityManager.GetPlayerController(nickname) != null)
                References.EntityManager.UnloadPlayerController(nickname);

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

            List<ulong> addedList = added.ToList();
            List<ulong> removedList = removed.ToList();

            bool sentAlready = false;
            const int MAX_RECORDS = NearbyEntities.MAX_RECORDS;
            for (int pos = 0; true; pos += MAX_RECORDS)
            {
                int sizeAdd = System.Math.Clamp(addedList.Count - pos, 0, MAX_RECORDS);
                int sizeRem = System.Math.Clamp(removedList.Count - pos, 0, MAX_RECORDS);

                if (sizeAdd == 0 && sizeRem == 0) // no data
                    if (sentAlready || !sendAtLeastOne) // no need to send empty packet
                        break;

                NearbyEntities nearbyEntities = new NearbyEntities(
                    fixedFrame,
                    sizeAdd != 0 ? addedList.GetRange(pos, sizeAdd).ToList() : new(),
                    sizeRem != 0 ? removedList.GetRange(pos, sizeRem).ToList() : new()
                    );
                if (!nearbyEntities.HasProblems)
                {
                    Packet packet = nearbyEntities.GetPacket();
                    References.Server.Send(nickname, packet);
                    sentAlready = true;
                }
                else throw new System.Exception("Couldn't construct NearbyEntities packet!");
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

            if (References.EntityManager.GetPlayerController(nickname) != null)
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

        public Vector2 GetPlayerRenderingPosition(string nickname)
        {
            PlayerState state = GetPlayerState(nickname);

            switch(state)
            {
                case PlayerState.Inactive:
                case PlayerState.Alive:
                    return References.EntityManager.GetPlayerController(nickname).EntityData.Position;

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
