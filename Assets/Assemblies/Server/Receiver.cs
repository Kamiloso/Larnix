using System.Collections;
using System.Collections.Generic;
using Larnix.Packets;
using Larnix.Server.Entities;
using Larnix.Server.Terrain;
using Larnix.Core.Utils;
using Larnix.Socket.Backend;
using Larnix.Core.References;
using Larnix.Socket.Packets.Control;
using Larnix.Core.Vectors;

namespace Larnix.Server
{
    internal class Receiver : Singleton
    {
        private WorldAPI WorldAPI => Ref<WorldAPI>();
        private QuickServer QuickServer => Ref<QuickServer>();
        private PlayerManager PlayerManager => Ref<PlayerManager>();
        private BlockSender BlockSender => Ref<BlockSender>();

        public Receiver(Server server) : base(server)
        {
            QuickServer.Subscribe<AllowConnection>(_AllowConnection);
            QuickServer.Subscribe<Stop>(_Stop);
            QuickServer.Subscribe<PlayerUpdate>(_PlayerUpdate);
            QuickServer.Subscribe<CodeInfo>(_CodeInfo);
            QuickServer.Subscribe<BlockChange>(_BlockChange);
        }

        private void _AllowConnection(AllowConnection msg, string owner)
        {
            // Create player connection
            PlayerManager.JoinPlayer(owner);

            // Info to console
            Core.Debug.Log(owner + " joined the game.");
        }

        private void _Stop(Stop msg, string owner)
        {
            // Remove player connection
            PlayerManager.DisconnectPlayer(owner);

            // Info to console
            Core.Debug.Log(owner + " disconnected.");
        }

        private void _PlayerUpdate(PlayerUpdate msg, string owner)
        {
            // check if most recent data (fast mode receiving - over raw udp)
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
                bool in_chunk = PlayerManager.PlayerHasChunk(owner, chunk);
                bool can_place = WorldAPI.CanPlaceBlock(POS, front, msg.Item);

                bool success = has_item && in_chunk && can_place;

                if (success)
                {
                    WorldAPI.PlaceBlockWithEffects(POS, front, msg.Item);
                }

                BlockSender.AddRetBlockChange(new BlockChangeItem(owner, msg.Operation, POS, front, success));
            }

            else if (code == 1) // break using item
            {
                bool has_tool = true;
                bool in_chunk = PlayerManager.PlayerHasChunk(owner, chunk);
                bool can_break = WorldAPI.CanBreakBlock(POS, front, msg.Item, msg.Tool);

                bool success = has_tool && in_chunk && can_break;

                if (success)
                {
                    WorldAPI.BreakBlockWithEffects(POS, front, msg.Tool);
                }

                BlockSender.AddRetBlockChange(new BlockChangeItem(owner, msg.Operation, POS, front, success));
            }
        }
    }
}
