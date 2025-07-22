using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Socket.Commands;

namespace Larnix.Server
{
    public class PlayerManager : MonoBehaviour
    {
        public readonly Dictionary<string, ulong> PlayerUID = new();
        public readonly Dictionary<string, PlayerUpdate> RecentPlayerUpdates = new(); // valid even for dead players
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
            CreatePlayerInstance(nickname);
            ulong uid = References.EntityManager.GetPlayerController(nickname).uID;
            PlayerUID[nickname] = uid;
        }

        public void CreatePlayerInstance(string nickname)
        {
            References.EntityManager.CreatePlayerController(nickname);
        }

        public void UpdatePlayerDataIfHasController(string nickname, PlayerUpdate msg)
        {
            EntityController playerController = References.EntityManager.GetPlayerController(nickname);
            if (playerController != null) // Player is either PlayerState.Inactive or PlayerState.Alive
            {
                // Load data to player controller
                playerController.ActivateIfNotActive();
                Entities.EntityData entityData = playerController.EntityData.ShallowCopy();
                entityData.Position = msg.Position;
                entityData.Rotation = msg.Rotation;
                playerController.UpdateEntityData(entityData);

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
