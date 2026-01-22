using System.Collections;
using System.Collections.Generic;
using Larnix.Socket.Packets.Game;
using Larnix.Server.Entities;
using Larnix.Server.Terrain;
using Larnix.Core.Utils;
using Larnix.Socket.Backend;
using Larnix.Server.References;
using Larnix.Socket.Packets.Control;
using Larnix.Core.Vectors;

namespace Larnix.Server
{
    internal class Receiver : ServerSingleton
    {
        private WorldAPI WorldAPI => Ref<ChunkLoading>().WorldAPI;

        public Receiver(Server server) : base(server)
        {
            QuickServer quickServer = Ref<QuickServer>();

            quickServer.Subscribe<AllowConnection>(_AllowConnection);
            quickServer.Subscribe<Stop>(_Stop);
            quickServer.Subscribe<PlayerUpdate>(_PlayerUpdate);
            quickServer.Subscribe<CodeInfo>(_CodeInfo);
            quickServer.Subscribe<BlockChange>(_BlockChange);
        }

        private void _AllowConnection(AllowConnection msg, string owner)
        {
            // Create player connection
            Ref<PlayerManager>().JoinPlayer(owner);

            // Info to console
            Core.Debug.Log(owner + " joined the game.");
        }

        private void _Stop(Stop msg, string owner)
        {
            // Remove player connection
            Ref<PlayerManager>().DisconnectPlayer(owner);

            // Info to console
            Core.Debug.Log(owner + " disconnected.");
        }

        private void _PlayerUpdate(PlayerUpdate msg, string owner)
        {
            // check if most recent data (fast mode receiving - over raw udp)
            PlayerUpdate lastPacket = Ref<PlayerManager>().GetRecentPlayerUpdate(owner);
            if (lastPacket == null || lastPacket.FixedFrame < msg.FixedFrame)
            {
                Ref<PlayerManager>().UpdatePlayerDataIfHasController(owner, msg);
            }
        }

        private void _CodeInfo(CodeInfo msg, string owner)
        {
            CodeInfo.Info code = msg.Code;

            if (code == CodeInfo.Info.RespawnMe)
            {
                if (Ref<PlayerManager>().GetPlayerState(owner) == PlayerManager.PlayerState.Dead)
                    Ref<PlayerManager>().CreatePlayerInstance(owner);
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
                bool in_chunk = Ref<PlayerManager>().PlayerHasChunk(owner, chunk);
                bool can_place = WorldAPI.CanPlaceBlock(POS, front, msg.Item);

                bool success = has_item && in_chunk && can_place;

                if (success)
                {
                    WorldAPI.PlaceBlockWithEffects(POS, front, msg.Item);
                }

                Ref<BlockSender>().AddRetBlockChange(owner, msg.Operation, POS, front, success);
            }

            else if (code == 1) // break using item
            {
                bool has_tool = true;
                bool in_chunk = Ref<PlayerManager>().PlayerHasChunk(owner, chunk);
                bool can_break = WorldAPI.CanBreakBlock(POS, front, msg.Item, msg.Tool);

                bool success = has_tool && in_chunk && can_break;

                if (success)
                {
                    WorldAPI.BreakBlockWithEffects(POS, front, msg.Tool);
                }

                Ref<BlockSender>().AddRetBlockChange(owner, msg.Operation, POS, front, success);
            }
        }
    }
}
