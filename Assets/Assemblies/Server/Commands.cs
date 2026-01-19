using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks;
using Larnix.Entities;
using Larnix.Server.Entities;
using System;
using Larnix.Server.Terrain;
using System.Text;
using Larnix.Packets.Game;
using Larnix.Core.Vectors;
using Larnix.Blocks.Structs;
using Larnix.Entities.Structs;
using Larnix.Socket.Backend;
using Larnix.Worldgen;
using Larnix.Server.References;
using Console = Larnix.Core.Console;

namespace Larnix.Server
{
    internal class Commands : ServerSingleton
    {
        private WorldAPI WorldAPI => Ref<ChunkLoading>().WorldAPI;

        public enum CommandResultType
        {
            Raw,
            Log,
            Success,
            Warning,
            Error,
            Ignore
        }

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
                    case CommandResultType.Raw: Console.LogRaw(message); break;
                    case CommandResultType.Log: Console.Log(message); break;
                    case CommandResultType.Success: Console.LogSuccess(message); break;
                    case CommandResultType.Warning: Console.LogWarning(message); break;
                    case CommandResultType.Error: Console.LogError(message); break;
                    case CommandResultType.Ignore: break;
                }
            }
            else
            {
                throw new NotImplementedException("Only server can execute commands for now.");
            }
        }

        private (CommandResultType, string) Execute(string command, string sender = null)
        {
            string[] arg = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int len = arg.Length;

            if (len == 0)
                return (CommandResultType.Error, "Unknown command! Type 'help' for documentation.");

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
                _ => (CommandResultType.Error, "Unknown command! Type 'help' for documentation.")
            };
        }

        private (CommandResultType, string) Help()
        {
            return (CommandResultType.Raw,
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

        private (CommandResultType, string) Stop()
        {
            Ref<Server>().CloseServer();
            return (CommandResultType.Ignore, string.Empty);
        }

        private (CommandResultType, string) PlayerList()
        {
            StringBuilder sb = new();
            sb.Append("\n");
            sb.Append($" | ------ PLAYER LIST [ {Ref<QuickServer>().CountPlayers()} / {Ref<QuickServer>().Config.MaxClients} ] ------\n");
            sb.Append(" |\n");

            foreach (string nickname in Ref<PlayerManager>().GetAllPlayerNicknames())
            {
                sb.Append($" | {nickname} from {Ref<QuickServer>().GetClientEndPoint(nickname)}" +
                          $" is {Ref<PlayerManager>().GetPlayerState(nickname).ToString().ToUpper()}\n");
            }

            sb.Append("\n");

            return (CommandResultType.Raw, sb.ToString());
        }

        private (CommandResultType, string) Tp(string nickname, string xt, string yt)
        {
            if (Ref<PlayerManager>().GetPlayerState(nickname) == PlayerManager.PlayerState.Alive)
            {
                if (double.TryParse(xt, out double x) && double.TryParse(yt, out double y))
                {
                    Vec2 targetPos = new Vec2(x, y);
                    Vec2 normalOffset = new Vec2(0.00, 0.01);
                    Vec2 fullTargetPos = targetPos + normalOffset;
                    Ref<QuickServer>().Send(nickname, new Teleport(fullTargetPos));
                    ((Player)Ref<EntityManager>().GetPlayerController(nickname).GetRealController()).AcceptTeleport(fullTargetPos);
                    return (CommandResultType.Success, "Player " + nickname + " has been teleported to " + targetPos);
                }
                else return (CommandResultType.Error, "Cannot parse coordinates!");
            }
            else
            {
                return (CommandResultType.Error, "Player " + nickname + " is not alive!");
            }
        }

        private (CommandResultType, string) Kick(string nickname)
        {
            if (Ref<PlayerManager>().GetPlayerState(nickname) != PlayerManager.PlayerState.None)
            {
                Ref<QuickServer>().FinishConnection(nickname);
                return (CommandResultType.Success, "Player " + nickname + " has been kicked.");
            }
            else
            {
                return (CommandResultType.Error, "Player " + nickname + " is not online!");
            }
        }

        private (CommandResultType, string) Kill(string nickname)
        {
            if (Ref<PlayerManager>().GetPlayerState(nickname) == PlayerManager.PlayerState.Alive)
            {
                ulong uid = Ref<PlayerManager>().GetPlayerUID(nickname);
                Ref<EntityManager>().KillEntity(uid);
                return (CommandResultType.Success, "Player " + nickname + " has been killed.");
            }
            else
            {
                return (CommandResultType.Error, "Player " + nickname + " is not alive!");
            }
        }

        private (CommandResultType, string) Spawn(string entityname, string xs, string ys)
        {
            if (Enum.TryParse(entityname, ignoreCase: true, out EntityID entityID) &&
                Enum.IsDefined(typeof(EntityID), entityID) &&
                entityID != EntityID.Player)
            {
                if (double.TryParse(xs, out double x) && double.TryParse(ys, out double y))
                {
                    Ref<EntityManager>().SummonEntity(new EntityData
                    {
                        ID = entityID,
                        Position = new Vec2(x, y)
                    });
                    return (CommandResultType.Success, $"Spawned {entityname} at position ({x}, {y}).");
                }
                else return (CommandResultType.Error, "Cannot parse coordinates!");
            }
            else return (CommandResultType.Error, $"Cannot spawn entity named \"{entityname}\"!");
        }

        private (CommandResultType, string) Place(string[] arg)
        {
            bool front = arg[1] == "front";
            if (arg[1] != "front" && arg[1] != "back")
            {
                return (CommandResultType.Error, $"Phrase '{arg[1]}' is not valid in this context.");
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
                    var result = WorldAPI.ReplaceBlock(new Vec2Int(x, y), front, new BlockData1
                    {
                        ID = blockID,
                        Variant = variant
                    });

                    if (result != null)
                        return (CommandResultType.Success,
                            $"Block ({x}, {y}) changed to " + blockID.ToString() + (variant == 0 ? "" : "-" + variant));
                    else
                        return (CommandResultType.Error, $"Position ({x}, {y}) cannot be updated.");
                }
                else return (CommandResultType.Error, $"Cannot place block named {blockname}!");
            }
            else return (CommandResultType.Error, "Cannot parse coordinates!");
        }

        private (CommandResultType, string) Seed()
        {
            long seed = Ref<Generator>().Seed;
            return (CommandResultType.Log, "Seed: " + seed);
        }
    }
}
