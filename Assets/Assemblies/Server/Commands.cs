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
using Larnix.Core.References;
using Console = Larnix.Core.Console;

namespace Larnix.Server
{
    internal class Commands : Singleton
    {
        private QuickServer QuickServer => Ref<QuickServer>();
        private PlayerManager PlayerManager => Ref<PlayerManager>();
        private EntityManager EntityManager => Ref<EntityManager>();
        private Generator Generator => Ref<Generator>();
        private Server Server => Ref<Server>();
        private WorldAPI WorldAPI => Ref<ChunkLoading>().WorldAPI;

        public enum ResultType { Raw, Log, Success, Warning, Error, Ignore }
        public Commands(Server server) : base(server) { }

        public override void PostEarlyFrameUpdate()
        {
            while (true)
            {
                string cmd = Console.GetCommand();
                if (cmd == null) break;

                ExecuteAndInform(cmd, null);
            }
        }

        public void ExecuteAndInform(string command, string sender)
        {
            var (type, message) = Execute(command, sender);

            if (sender == null)
            {
                switch (type)
                {
                    case ResultType.Raw: Console.LogRaw(message); break;
                    case ResultType.Log: Console.Log(message); break;
                    case ResultType.Success: Console.LogSuccess(message); break;
                    case ResultType.Warning: Console.LogWarning(message); break;
                    case ResultType.Error: Console.LogError(message); break;
                    case ResultType.Ignore: break;
                }
            }
            else
            {
                throw new NotImplementedException("Only server can execute commands for now.");
            }
        }

        private (ResultType, string) Execute(string command, string sender = null)
        {
            string[] arg = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int len = arg.Length;

            if (len == 0)
                return (ResultType.Error, "Unknown command! Type 'help' for documentation.");

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
                "seed" when len == 1 => Seed(),
                _ => (ResultType.Error, "Unknown command! Type 'help' for documentation.")
            };
        }

        private (ResultType, string) Help()
        {
            return (ResultType.Raw,
                "\n" +
                " | ------ COMMAND LIST ------\n" +
                " |\n" +
                " | help - Displays this documentation.\n" +
                " | stop - Turns off the server.\n" +
                " | playerlist - Shows all players on the server.\n" +
                " | tp [nickname] [x] [y] - Teleports player to a given position.\n" +
                " | kick [nickname] - Kicks player if online.\n" +
                " | kill [nickname] - Kills player if alive.\n" +
                " | spawn [entity] [x] [y] - Spawns entity at a given position.\n" +
                " | place [front/back] [x] [y] [block] [?variant] - Places block at a given position.\n" +
                " | seed - Displays the server seed.\n" +
                "\n");
        }

        private (ResultType, string) Stop()
        {
            Server.CloseServer();
            return (ResultType.Ignore, string.Empty);
        }

        private (ResultType, string) PlayerList()
        {
            StringBuilder sb = new();
            sb.Append("\n");
            sb.Append($" | ------ PLAYER LIST [ {QuickServer.PlayerCount} / {QuickServer.Config.MaxClients} ] ------\n");
            sb.Append(" |\n");

            foreach (string nickname in PlayerManager.GetAllPlayerNicknames())
            {
                sb.Append($" | {nickname} from {QuickServer.GetClientEndPoint(nickname)}" +
                          $" is {PlayerManager.GetPlayerState(nickname).ToString().ToUpper()}\n");
            }

            sb.Append("\n");

            return (ResultType.Raw, sb.ToString());
        }

        private (ResultType, string) Tp(string nickname, string xt, string yt)
        {
            if (PlayerManager.GetPlayerState(nickname) == PlayerManager.PlayerState.Alive)
            {
                if (double.TryParse(xt, out double x) && double.TryParse(yt, out double y))
                {
                    Vec2 targetPos = new Vec2(x, y);
                    Vec2 normalOffset = new Vec2(0.00, 0.01);
                    Vec2 fullTargetPos = targetPos + normalOffset;
                    QuickServer.Send(nickname, new Teleport(fullTargetPos));
                    ((Player)EntityManager.GetPlayerController(nickname).GetRealController()).AcceptTeleport(fullTargetPos);
                    return (ResultType.Success, "Player " + nickname + " has been teleported to " + targetPos);
                }
                else return (ResultType.Error, "Cannot parse coordinates!");
            }
            else
            {
                return (ResultType.Error, "Player " + nickname + " is not alive!");
            }
        }

        private (ResultType, string) Kick(string nickname)
        {
            if (PlayerManager.GetPlayerState(nickname) != PlayerManager.PlayerState.None)
            {
                QuickServer.FinishConnection(nickname);
                return (ResultType.Success, "Player " + nickname + " has been kicked.");
            }
            else
            {
                return (ResultType.Error, "Player " + nickname + " is not online!");
            }
        }

        private (ResultType, string) Kill(string nickname)
        {
            if (PlayerManager.GetPlayerState(nickname) == PlayerManager.PlayerState.Alive)
            {
                ulong uid = PlayerManager.GetPlayerUID(nickname);
                EntityManager.KillEntity(uid);
                return (ResultType.Success, "Player " + nickname + " has been killed.");
            }
            else
            {
                return (ResultType.Error, "Player " + nickname + " is not alive!");
            }
        }

        private (ResultType, string) Spawn(string entityname, string xs, string ys)
        {
            if (Enum.TryParse(entityname, ignoreCase: true, out EntityID entityID) &&
                Enum.IsDefined(typeof(EntityID), entityID) &&
                entityID != EntityID.Player)
            {
                if (double.TryParse(xs, out double x) && double.TryParse(ys, out double y))
                {
                    EntityManager.SummonEntity(new EntityData(
                        id: entityID,
                        position: new Vec2(x, y),
                        rotation: 0.0f,
                        data: null
                    ));
                    return (ResultType.Success, $"Spawned {entityname} at position ({x}, {y}).");
                }
                else return (ResultType.Error, "Cannot parse coordinates!");
            }
            else return (ResultType.Error, $"Cannot spawn entity named \"{entityname}\"!");
        }

        private (ResultType, string) Place(string[] arg)
        {
            bool front = arg[1] == "front";
            if (arg[1] != "front" && arg[1] != "back")
            {
                return (ResultType.Error, $"Phrase '{arg[1]}' is not valid in this context.");
            }

            if (int.TryParse(arg[2], out int x) && int.TryParse(arg[3], out int y))
            {
                string blockname = arg[4];

                byte variant;
                if (arg.Length == 5 || !byte.TryParse(arg[5], out variant))
                    variant = 0;

                if (Enum.TryParse(blockname, ignoreCase: true, out BlockID blockID) &&
                    Enum.IsDefined(typeof(BlockID), blockID))
                {
                    var result = WorldAPI.ReplaceBlock(new Vec2Int(x, y), front, new BlockData1(
                        id: blockID,
                        variant: variant,
                        data: null
                    ));

                    if (result != null)
                        return (ResultType.Success,
                            $"Block ({x}, {y}) changed to " + blockID.ToString() + (variant == 0 ? "" : "-" + variant));
                    else
                        return (ResultType.Error, $"Position ({x}, {y}) cannot be updated.");
                }
                else return (ResultType.Error, $"Cannot place block named {blockname}!");
            }
            else return (ResultType.Error, "Cannot parse coordinates!");
        }

        private (ResultType, string) Seed()
        {
            long seed = Generator.Seed;
            return (ResultType.Log, "Seed: " + seed);
        }
    }
}
