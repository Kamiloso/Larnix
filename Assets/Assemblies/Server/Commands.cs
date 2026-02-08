using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks;
using Larnix.Entities;
using Larnix.Server.Entities;
using System;
using Larnix.Server.Terrain;
using System.Text;
using Larnix.Packets;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using Larnix.Entities.Structs;
using Larnix.Socket.Backend;
using Larnix.Worldgen;
using Larnix.Core;
using Larnix.Socket.Packets;
using Larnix.Core.Utils;
using Larnix.Server.Data;
using Console = Larnix.Core.Console;
using CmdResult = Larnix.Core.ICmdExecutor.CmdResult;
using System.Net;

namespace Larnix.Server
{
    internal class Commands : Singleton, ICmdExecutor
    {
        private QuickServer QuickServer => Ref<QuickServer>();
        private PlayerManager PlayerManager => Ref<PlayerManager>();
        private EntityManager EntityManager => Ref<EntityManager>();
        private Generator Generator => Ref<Generator>();
        private Server Server => Ref<Server>();
        private Config Config => Ref<Config>();
        private WorldAPI WorldAPI => Ref<ChunkLoading>().WorldAPI;

        public Commands(Server server) : base(server) { }

        public override void PostEarlyFrameUpdate()
        {
            while (true)
            {
                string cmd = Console.GetCommand();
                if (cmd == null) break;

                var (type, message) = ExecuteCommand(cmd);
                switch (type)
                {
                    case CmdResult.Raw: Console.LogRaw(message); break;
                    case CmdResult.Log: Console.Log(message); break;
                    case CmdResult.Success: Console.LogSuccess(message); break;
                    case CmdResult.Warning: Console.LogWarning(message); break;
                    case CmdResult.Error: Console.LogError(message); break;
                    case CmdResult.Ignore: break;
                }
            }
        }

        public (CmdResult, string) ExecuteCommand(string command, string sender = null)
        {
            CmdResult type = CmdResult.Ignore;
            string message = string.Empty;

            if (sender is null) // from console
            {
                (type, message) = InnerExecuteCmd(command, sender, true);
            }
            else // from player
            {
                bool player_online = PlayerManager.GetPlayerState(sender) != PlayerManager.PlayerState.None;
                if (player_online)
                {
                    bool player_admin = /*Config.AdminList.Contains(sender)*/ false;
                    (type, message) = InnerExecuteCmd(command, sender, player_admin);

                    if (type != CmdResult.Ignore)
                    {
                        //QuickServer.Send(sender, new ChatMessage(type, (String512)message));
                    }
                }
            }

            return (type, message);
        }

        private (CmdResult, string) InnerExecuteCmd(string command, string sender, bool adminPrivileges)
        {
            string[] arg = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int len = arg.Length;

            if (len == 0)
                return (CmdResult.Error, "Unknown command! Type 'help' for documentation.");

            if (!adminPrivileges && !(
                arg[0] == "help" //||
                //arg[0] == "..."
            ))
                return (CmdResult.Error, "You don't have permission to execute this command.");

            return arg[0] switch
            {
                "help" => Help(),
                "stop" when len == 1 => Stop(),
                "playerlist" when len == 1 => PlayerList(),
                "tp" when len == 4 => Tp(arg[1], arg[2], arg[3]),
                "kick" when len == 2 => Kick(arg[1]),
                "kill" when len == 2 => Kill(arg[1]),
                "spawn" when len == 4 => Spawn(arg[1], arg[2], arg[3]),
                "place" when len == 5 || len == 6 => Place(arg),
                "particles" when len == 4 => Particles(arg[1], arg[2], arg[3]),
                "seed" when len == 1 => Seed(),
                _ => (CmdResult.Error, "Unknown command! Type 'help' for documentation.")
            };
        }

        private (CmdResult, string) Help()
        {
            return (CmdResult.Raw,
@"
 | ------ COMMAND LIST ------
 |
 | help - Displays this documentation.
 | stop - Turns off the server.
 | playerlist - Shows all players on the server.
 | tp [nickname] [x] [y] - Teleports player to a given position.
 | kick [nickname] - Kicks player if online.
 | kill [nickname] - Kills player if alive.
 | spawn [entity] [x] [y] - Spawns entity at a given position.
 | place [front/back] [x] [y] [block] [?variant] - Places block at a given position.
 | particles [name] [x] [y] - Spawns particles at a given position.
 | seed - Displays the server seed.

");
        }

        private (CmdResult, string) Stop()
        {
            Server.CloseServer();
            return (CmdResult.Ignore, string.Empty);
        }

        private (CmdResult, string) PlayerList()
        {
            StringBuilder sb = new();
            sb.Append("\n");
            sb.Append($" | ------ PLAYER LIST [ {QuickServer.PlayerCount} / {QuickServer.Config.MaxClients} ] ------\n");
            sb.Append(" |\n");

            foreach (string nickname in PlayerManager.GetAllPlayerNicknames())
            {
                IPEndPoint endPoint = QuickServer.GetClientEndPoint(nickname);
                string playerState = PlayerManager.GetPlayerState(nickname).ToString().ToUpper();
                sb.Append($" | {nickname} from {endPoint} is {playerState}\n");
            }

            sb.Append("\n");

            return (CmdResult.Raw, sb.ToString());
        }

        private (CmdResult, string) Tp(string nickname, string xt, string yt)
        {
            if (PlayerManager.GetPlayerState(nickname) != PlayerManager.PlayerState.Alive)
                return (CmdResult.Error, $"Player {nickname} is not alive.");

            if (!DoubleUtils.TryParse(xt, out double x) || !DoubleUtils.TryParse(yt, out double y))
                return (CmdResult.Error, "Cannot parse coordinates.");

            Vec2 targetPos = new Vec2(x, y);
            Vec2 normalOffset = new Vec2(0.00, 0.01);
            Vec2 fullTargetPos = targetPos + normalOffset;

            QuickServer.Send(nickname, new Teleport(fullTargetPos));
            ((Player)EntityManager.GetPlayerController(nickname).GetRealController()).AcceptTeleport(fullTargetPos);
            return (CmdResult.Success, $"Player {nickname} has been teleported to {targetPos}.");
        }

        private (CmdResult, string) Kick(string nickname)
        {
            if (PlayerManager.GetPlayerState(nickname) == PlayerManager.PlayerState.None)
                return (CmdResult.Error, $"Player {nickname} is not online.");

            QuickServer.FinishConnection(nickname);
            return (CmdResult.Success, $"Player {nickname} has been kicked.");
        }

        private (CmdResult, string) Kill(string nickname)
        {
            if (PlayerManager.GetPlayerState(nickname) != PlayerManager.PlayerState.Alive)
                return (CmdResult.Error, $"Player {nickname} is not alive.");

            ulong uid = PlayerManager.GetPlayerUID(nickname);
            EntityManager.KillEntity(uid);
            return (CmdResult.Success, $"Player {nickname} has been killed.");
        }

        private (CmdResult, string) Spawn(string entityname, string xs, string ys)
        {
            if (!Enum.TryParse(entityname, ignoreCase: true, out EntityID entityID) ||
                !Enum.IsDefined(typeof(EntityID), entityID))
                return (CmdResult.Error, $"Cannot spawn entity named \"{entityname}\".");

            if (entityID == EntityID.Player)
                return (CmdResult.Error, "Player is not a spawnable entity.");

            if (!DoubleUtils.TryParse(xs, out double x) || !DoubleUtils.TryParse(ys, out double y))
                return (CmdResult.Error, "Cannot parse coordinates.");

            Vec2 position = new Vec2(x, y);

            bool success = EntityManager.SummonEntity(new EntityData(
                id: entityID,
                position: position,
                rotation: 0.0f,
                data: null
            ));

            if (!success)
                return (CmdResult.Error, $"Position {position} is not loaded.");

            string realName = entityID.ToString();
            return (CmdResult.Success, $"Spawned {realName} at position {position}.");
        }

        private (CmdResult, string) Place(string[] arg)
        {
            bool front = arg[1] == "front";
            if (arg[1] != "front" && arg[1] != "back")
            {
                return (CmdResult.Error, $"Phrase \"{arg[1]}\" is not valid in this context.");
            }

            if (!int.TryParse(arg[2], out int x) || !int.TryParse(arg[3], out int y))
                return (CmdResult.Error, "Cannot parse coordinates.");

            string blockname = arg[4];

            byte variant;
            if (arg.Length == 5 || !byte.TryParse(arg[5], out variant))
                variant = 0;

            if (!Enum.TryParse(blockname, ignoreCase: true, out BlockID blockID) ||
                !Enum.IsDefined(typeof(BlockID), blockID))
                return (CmdResult.Error, $"Cannot place block named \"{blockname}\".");

            Vec2Int POS = new Vec2Int(x, y);

            var result = WorldAPI.ReplaceBlock(POS, front, new BlockData1(
                id: blockID,
                variant: variant,
                data: null
            ), IWorldAPI.BreakMode.Replace);

            if (result != null)
            {
                string blockText = variant == 0 ? blockID.ToString() : $"{blockID}-{variant}";
                return (CmdResult.Success, $"Block at {POS} changed to {blockText}.");
            }

            return (CmdResult.Error, $"Position {POS} cannot be updated.");
        }

        private (CmdResult, string) Particles(string name, string xs, string ys)
        {
            if (!Enum.TryParse(name, ignoreCase: true, out ParticleID particleID) ||
                !Enum.IsDefined(typeof(ParticleID), particleID))
                return (CmdResult.Error, $"Cannot spawn particles named \"{name}\".");

            if (!DoubleUtils.TryParse(xs, out double x) || !DoubleUtils.TryParse(ys, out double y))
                return (CmdResult.Error, "Cannot parse coordinates.");

            Vec2 position = new Vec2(x, y);

            Payload particles = new SpawnParticles(
                position: position,
                particleID: particleID
            );

            IEnumerable<string> nearbyPlayers = PlayerManager.AllObserversInRange(position, Common.PARTICLE_VIEW_DISTANCE);
            foreach (string nickname in nearbyPlayers)
            {
                QuickServer.Send(nickname, particles);
            }

            string realName = particleID.ToString();
            Vec2 dblPos = new(x, y);
            return (CmdResult.Success, $"Spawned {realName} particles at position {dblPos}.");
        }

        private (CmdResult, string) Seed()
        {
            long seed = Generator.Seed;
            return (CmdResult.Log, $"Seed: {seed}");
        }
    }
}
