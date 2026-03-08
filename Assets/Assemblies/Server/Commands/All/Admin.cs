using System;
using System.Collections.Generic;
using Larnix.Core;
using Larnix.GameCore.Utils;
using Larnix.Server.Configuration;
using Larnix.GameCore.Json;
using CmdResult = Larnix.GameCore.ICmdExecutor.CmdResult;

namespace Larnix.Server.Commands.All
{
    internal class Admin : BaseCmd
    {
        public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Admin;
        public override string Pattern => $"{Name} <params...>";
        public override string Hint => $"Type 'help {Name}' for more information.";
        public override string ShortDescription => "Manages administrative privileges.";
        
        public override string LongDescription => ShortDescription + " Usage:\n" +
            "admin add <nickname> - Grants admin privileges to a player.\n" +
            "admin remove <nickname> - Revokes admin privileges.\n" +
            "admin list - Lists all admin entries.\n" +
            "admin clear - Clears the admin list.";

        private Server Server => GlobRef.Get<Server>();
        private ServerConfig ServerConfig => GlobRef.Get<ServerConfig>();

        private string _subname;
        private string _nickname;

        public override void Inject(string command)
        {
            if (!TrySplitMin(command, 2, out string[] parts, out int length))
                throw FormatException(InvalidCmdFormat);

            string subname = parts[1];
            string subcommand = string.Join(' ', parts[1..]);
            _subname = subname;

            _nickname = null;

            var commands = new Dictionary<string, Action<string[]>>
            {
                ["add"] = args =>
                {
                    if (!Parsing.TryParseNickname(args[1], out var nick)) throw FormatException(Validation.WrongNicknameInfo);
                    _nickname = nick;
                },
                ["remove"] = args =>
                {
                    if (!Parsing.TryParseNickname(args[1], out var nick)) throw FormatException(Validation.WrongNicknameInfo);
                    _nickname = nick;
                },
                ["list"] = args => { },
                ["clear"] = args => { },
            };

            if (!commands.TryGetValue(subname, out var action))
                throw FormatException(InvalidCmdFormat);

            int expectedArgs = subname switch
            {
                "add" or "remove" => 2,
                "list" or "clear" => 1,
                _ => throw FormatException(InvalidCmdFormat)
            };

            if (!TrySplit(subcommand, expectedArgs, out string[] cmdParts))
                throw FormatException(InvalidCmdFormat);

            action(cmdParts);
        }

        public override (CmdResult, string) Execute(string sender, PrivilegeLevel privilege)
        {
            var executes = new Dictionary<string, Func<(CmdResult, string)>>
            {
                ["add"] = ExecuteAdd,
                ["remove"] = ExecuteRemove,
                ["list"] = ExecuteList,
                ["clear"] = ExecuteClear,
            };

            if (!executes.TryGetValue(_subname, out var execute))
            {
                return (CmdResult.Error, "Invalid subcommand.");
            }
            
            return execute();
        }

        private (CmdResult, string) ExecuteAdd()
        {
            if (!ServerConfig.Administration_Admins.Contains(_nickname))
            {
                ServerConfig.Administration_Admins.Add(_nickname);
                Config.ToDirectory(Server.WorldPath, ServerConfig);

                return (CmdResult.Success,
                    $"Successfully granted admin privileges to '{_nickname}'.");
            }

            return (CmdResult.Error,
                $"'{_nickname}' already has admin privileges.");
        }

        private (CmdResult, string) ExecuteRemove()
        {
            if (ServerConfig.Administration_Admins.Contains(_nickname))
            {
                ServerConfig.Administration_Admins.Remove(_nickname);
                Config.ToDirectory(Server.WorldPath, ServerConfig);

                return (CmdResult.Success,
                    $"Successfully revoked admin privileges from '{_nickname}'.");
            }

            return (CmdResult.Error,
                $"'{_nickname}' does not have admin privileges.");
        }

        private (CmdResult, string) ExecuteList()
        {
            return (CmdResult.Raw,
                MakeRobustList("ADMIN ENTRIES", "'", ServerConfig.Administration_Admins, "'"));
        }

        private (CmdResult, string) ExecuteClear()
        {
            ServerConfig.Administration_Admins.Clear();
            Config.ToDirectory(Server.WorldPath, ServerConfig);

            return (CmdResult.Success,
                "Successfully cleared the admin list.");
        }
    }
}
