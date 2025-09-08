using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QuickNet.Channel.Cmds;
using Larnix.Blocks;
using Larnix.Server.Entities;
using Larnix.Server.Terrain;
using Larnix.Packets;

namespace Larnix.Server
{
    public class Receiver
    {
        private WorldAPI WorldAPI => Ref.ChunkLoading.WorldAPI;

        public Receiver(QuickNet.Backend.QuickServer server)
        {
            server.Subscribe<AllowConnection>(_AllowConnection);
            server.Subscribe<Stop>(_Stop);
            server.Subscribe<PlayerUpdate>(_PlayerUpdate);
            server.Subscribe<CodeInfo>(_CodeInfo);
            server.Subscribe<BlockChange>(_BlockChange);
        }

        private void _AllowConnection(AllowConnection msg, string owner)
        {
            // Create player connection
            Ref.PlayerManager.JoinPlayer(owner);

            // Info to console
            Core.Debug.Log(owner + " joined the game.");
        }

        private void _Stop(Stop msg, string owner)
        {
            // Remove player connection
            Ref.PlayerManager.DisconnectPlayer(owner);

            // Info to console
            Core.Debug.Log(owner + " disconnected.");
        }

        private void _PlayerUpdate(PlayerUpdate msg, string owner)
        {
            // check if most recent data (fast mode receiving - over raw udp)
            Dictionary<string, PlayerUpdate> RecentPlayerUpdates = Ref.PlayerManager.RecentPlayerUpdates;
            if (!RecentPlayerUpdates.ContainsKey(owner) || RecentPlayerUpdates[owner].FixedFrame < msg.FixedFrame)
            {
                // Update player data
                Ref.PlayerManager.UpdatePlayerDataIfHasController(owner, msg);
            }
        }

        private void _CodeInfo(CodeInfo msg, string owner)
        {
            CodeInfo.Info code = msg.Code;

            if (code == CodeInfo.Info.RespawnMe)
            {
                if (Ref.PlayerManager.GetPlayerState(owner) == PlayerManager.PlayerState.Dead)
                    Ref.PlayerManager.CreatePlayerInstance(owner);
            }
        }

        private void _BlockChange(BlockChange msg, string owner)
        {
            Vector2Int POS = msg.BlockPosition;
            Vector2Int chunk = ChunkMethods.CoordsToChunk(POS);
            bool front = msg.Front;
            byte code = msg.Code;

            if (code == 0) // place item
            {
                bool has_item = true;
                bool in_chunk = Ref.PlayerManager.PlayerHasChunk(owner, chunk);
                bool can_place = WorldAPI.CanPlaceBlock(POS, front, msg.Item);

                bool success = has_item && in_chunk && can_place;

                if (success)
                {
                    WorldAPI.PlaceBlockWithEffects(POS, front, msg.Item);
                }

                Ref.BlockSender.AddRetBlockChange(owner, msg.Operation, POS, front, success);
            }

            else if (code == 1) // break using item
            {
                bool has_tool = true;
                bool in_chunk = Ref.PlayerManager.PlayerHasChunk(owner, chunk);
                bool can_break = WorldAPI.CanBreakBlock(POS, front, msg.Item, msg.Tool);

                bool success = has_tool && in_chunk && can_break;

                if (success)
                {
                    WorldAPI.BreakBlockWithEffects(POS, front, msg.Tool);
                }

                Ref.BlockSender.AddRetBlockChange(owner, msg.Operation, POS, front, success);
            }
        }
    }
}
