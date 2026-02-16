using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks;
using Larnix.Entities;
using Larnix.Server.Entities;
using Larnix.Entities.All;
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
using System.Net;
using Larnix.Core.Json;
using Console = Larnix.Core.Console;
using CmdResult = Larnix.Core.ICmdExecutor.CmdResult;
using PlayerState = Larnix.Server.Entities.PlayerManager.PlayerState;

namespace Larnix.Server
{
    internal class Commands : Singleton, ICmdExecutor
    {
        public enum PrivilegeLevel { User, Admin, Console }

        private QuickServer QuickServer => Ref<QuickServer>();
        private PlayerManager PlayerManager => Ref<PlayerManager>();
        private EntityManager EntityManager => Ref<EntityManager>();
        private Generator Generator => Ref<Generator>();
        private Server Server => Ref<Server>();
        private Config Config => Ref<Config>();
        private WorldAPI WorldAPI => Ref<WorldAPI>();

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
                (type, message) = InnerExecuteCmd(command, sender, PrivilegeLevel.Console);

                if (type == CmdResult.Clear)
                {
                    Console.Clear();
                }
            }
            else // from player
            {
                bool player_online = PlayerManager.GetPlayerState(sender) != PlayerState.None;
                if (player_online)
                {
                    bool player_admin = /*Config.AdminList.Contains(sender)*/ false;
                    (type, message) = InnerExecuteCmd(command, sender, player_admin ?
                        PrivilegeLevel.Admin : PrivilegeLevel.User);

                    if (type != CmdResult.Ignore)
                    {
                        //QuickServer.Send(sender, new ChatMessage(type, (String512)message));
                    }
                }
            }

            return (type, message);
        }

        private (CmdResult, string) InnerExecuteCmd(string command, string sender, PrivilegeLevel privilegeLevel)
        {
            string[] arg = command.Split(' ');
            int len = arg.Length;

            if ((privilegeLevel == PrivilegeLevel.User && !new List<string> { "help" }.Contains(arg[0])) || // USER ALLOW
                (privilegeLevel == PrivilegeLevel.Admin && !new List<string> { "passwd" }.Contains(arg[0]))) // ADMIN DENY
            {
                return (CmdResult.Error, "You don't have permission to execute this command. Your permission level: " + privilegeLevel);
            }

            return arg[0] switch
            {
                "help" => Help(),
                "stop" when len == 1 => Stop(),
                "playerlist" when len == 1 => PlayerList(),
                "tp" when len == 4 => Tp(arg[1], arg[2], arg[3]),
                "kick" when len == 2 => Kick(arg[1]),
                "kill" when len == 2 => Kill(arg[1]),
                "spawn" when len >= 4 => Spawn(arg[1], arg[2], arg[3], len >= 5 ? arg[4..] : null),
                "place" when len >= 5 => Place(arg[1], arg[2], arg[3], arg[4], len >= 6 ? arg[5] : null, len >= 7 ? arg[6..] : null),
                "particles" when len == 4 => Particles(arg[1], arg[2], arg[3]),
                "passwd" when len == 3 => Passwd(arg[1], arg[2]),
                "clear" when len == 1 => Clear(),
                "info" when len == 1 => Info(),
                _ => (CmdResult.Error, "Unknown command! Type 'help' for documentation.")
            };
        }

        private (CmdResult, string) Help()
        {
            StringBuilder sb = new();
            sb.Append($"\n");
            sb.Append($" | ------ COMMAND LIST ------\n");
            sb.Append($" |\n");
            sb.Append($" | help - Displays this documentation.\n");
            sb.Append($" | stop - Turns off the server.\n");
            sb.Append($" | playerlist - Shows all players on the server.\n");
            sb.Append($" | tp [nickname] [x] [y] - Teleports player to a given position.\n");
            sb.Append($" | kick [nickname] - Kicks player if online.\n");
            sb.Append($" | kill [nickname] - Kills player if alive.\n");
            sb.Append($" | spawn [entity] [x] [y] [?json] - Spawns entity at a given position.\n");
            sb.Append($" | place [front/back] [x] [y] [block] [?variant] [?json] - Places block at a given position.\n");
            sb.Append($" | particles [name] [x] [y] - Spawns particles at a given position.\n");
            sb.Append($" | passwd [nickname] [password] - Synchroneously overrides login data for a given nickname.\n");
            sb.Append($" | clear - Clears the console.\n");
            sb.Append($" | info - Displays server information.\n");
            sb.Append($"\n");

            return (CmdResult.Raw, sb.ToString());
        }

        private (CmdResult, string) Stop()
        {
            Server.CloseServer();
            return (CmdResult.Ignore, string.Empty);
        }

        private (CmdResult, string) PlayerList()
        {
            StringBuilder sb = new();
            sb.Append($"\n");
            sb.Append($" | ------ PLAYER LIST [ {QuickServer.PlayerCount} / {QuickServer.Config.MaxClients} ] ------\n");
            sb.Append($" |\n");

            foreach (string nickname in PlayerManager.AllPlayers())
            {
                string playerState = PlayerManager.GetPlayerState(nickname).ToString().ToUpper();
                
                if (!QuickServer.TryGetClientEndPoint(nickname, out IPEndPoint endPoint))
                    endPoint = new IPEndPoint(IPAddress.Any, 0); // unknown

                sb.Append($" | {nickname} from {endPoint} is {playerState}\n");
            }

            sb.Append($"\n");

            return (CmdResult.Raw, sb.ToString());
        }

        private (CmdResult, string) Tp(string nickname, string xt, string yt)
        {
            if (PlayerManager.GetPlayerState(nickname) != PlayerState.Alive)
                return (CmdResult.Error, $"Player {nickname} is not alive.");

            if (!DoubleUtils.TryParse(xt, out double x) || !DoubleUtils.TryParse(yt, out double y))
                return (CmdResult.Error, "Cannot parse coordinates.");

            Vec2 targetPos = new Vec2(x, y);
            Vec2 fullTargetPos = targetPos + Common.UP_EPSILON;

            QuickServer.Send(nickname, new Teleport(fullTargetPos));
            ((Player)EntityManager.GetPlayerController(nickname).Controller).AcceptTeleport(fullTargetPos);
            return (CmdResult.Success, $"Player {nickname} has been teleported to {targetPos}.");
        }

        private (CmdResult, string) Kick(string nickname)
        {
            if (PlayerManager.GetPlayerState(nickname) == PlayerState.None)
                return (CmdResult.Error, $"Player {nickname} is not online.");

            QuickServer.FinishConnectionRequest(nickname);
            return (CmdResult.Success, $"Player {nickname} has been kicked.");
        }

        private (CmdResult, string) Kill(string nickname)
        {
            if (PlayerManager.GetPlayerState(nickname) != PlayerState.Alive)
                return (CmdResult.Error, $"Player {nickname} is not alive.");

            ulong uid = PlayerManager.GetPlayerUID(nickname);
            EntityManager.KillEntity(uid);
            return (CmdResult.Success, $"Player {nickname} has been killed.");
        }

        private (CmdResult, string) Spawn(string entityname, string xs, string ys, string[] jsonlist = null)
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
                data: ParseJsonList(jsonlist)
            ));

            if (!success)
                return (CmdResult.Error, $"Position {position} is not loaded.");

            string realName = entityID.ToString();
            return (CmdResult.Success, $"Spawned {realName} at position {position}.");
        }

        private (CmdResult, string) Place(string mode, string xs, string ys, string blockname, string vs = null, string[] jsonlist = null)
        {
            bool front = mode == "front";
            if (mode != "front" && mode != "back")
            {
                return (CmdResult.Error, $"Phrase \"{mode}\" is not valid in this context.");
            }

            if (!int.TryParse(xs, out int x) || !int.TryParse(ys, out int y))
                return (CmdResult.Error, "Cannot parse coordinates.");

            byte variant;
            if (vs == null || !byte.TryParse(vs, out variant))
                variant = 0;

            if (!Enum.TryParse(blockname, ignoreCase: true, out BlockID blockID) ||
                !Enum.IsDefined(typeof(BlockID), blockID))
                return (CmdResult.Error, $"Cannot place block named \"{blockname}\".");

            Vec2Int POS = new Vec2Int(x, y);

            var result = WorldAPI.ReplaceBlock(POS, front, new BlockData1(
                id: blockID,
                variant: variant,
                data: ParseJsonList(jsonlist)
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

        private (CmdResult, string) Passwd(string nickname, string password)
        {
            if (nickname == Common.LOOPBACK_ONLY_NICKNAME)
                return (CmdResult.Error, "This nickname is reserved.");

            if (password == Common.LOOPBACK_ONLY_PASSWORD)
                return (CmdResult.Error, "This password is reserved.");

            if (!Validation.IsGoodNickname(nickname))
                return (CmdResult.Error, Validation.WrongNicknameInfo);

            if (!Validation.IsGoodPassword(password))
                return (CmdResult.Error, Validation.WrongPasswordInfo);
    
            QuickServer.UserManager.ChangePasswordSync(nickname, password);
            return (CmdResult.Success, "Password for " + nickname + " has been set/updated.");
        }

        private (CmdResult, string) Clear()
        {
            return (CmdResult.Clear, string.Empty);
        }

        private (CmdResult, string) Info()
        {
            StringBuilder sb = new();
            sb.Append("\n");
            sb.Append($" | ------ SERVER INFO ------\n");
            sb.Append($" |\n");
            sb.Append($" | Version: {Core.Version.Current}\n");
            sb.Append($" | Players: {QuickServer.PlayerCount} / {QuickServer.Config.MaxClients}\n");
            sb.Append($" | Port: {QuickServer.Config.Port}\n");
            sb.Append($" | Authcode: {QuickServer.Authcode}\n");
            sb.Append($" | Seed: {Generator.Seed}\n");
            sb.Append($"\n");

            return (CmdResult.Raw, sb.ToString());
        }

        // ===== STATIC HELPERS =====

        private static Storage ParseJsonList(string[] jsonlist)
        {
            if (jsonlist == null)
            {
                return new Storage();
            }

            string json = string.Join(' ', jsonlist);
            return Storage.FromString(json);
        }
    }
}
