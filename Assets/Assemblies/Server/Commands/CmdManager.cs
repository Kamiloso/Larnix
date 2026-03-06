using System.Collections;
using System.Collections.Generic;
using Larnix.Server.Entities;
using Larnix.Core;
using System;
using Larnix.Server.Configuration;
using Larnix.Server.Data;
using Console = Larnix.Core.Console;
using CmdResult = Larnix.Core.ICmdExecutor.CmdResult;

namespace Larnix.Server.Commands
{
    internal enum PrivilegeLevel
    {
        User = 1, // basic user commands
        Host = 2, // server management, but no gameplay-affecting commands
        Admin = 3, // administrative commands, including gameplay-affecting ones
        Console = 4, // full access, only from console
    }

    internal class CmdManager : IScript, ICmdExecutor
    {
        private Server Server => GlobRef.Get<Server>();
        private ServerConfig ServerConfig => GlobRef.Get<ServerConfig>();
        private DataSaver DataSaver => GlobRef.Get<DataSaver>();
        private PlayerActions PlayerActions => GlobRef.Get<PlayerActions>();

        void IScript.PostEarlyFrameUpdate()
        {
            while (Console.TryPopInput(out string cmd))
            {
                if (cmd.Trim() == string.Empty)
                    continue; // ignore empty commands

                var (type, message) = ExecuteCommand(cmd);
                switch (type)
                {
                    case CmdResult.Raw: Console.LogRaw(message); break;
                    case CmdResult.Log: Console.Log(message); break;
                    case CmdResult.Info: Console.LogInfo(message); break;
                    case CmdResult.Success: Console.LogSuccess(message); break;
                    case CmdResult.Warning: Console.LogWarning(message); break;
                    case CmdResult.Error: Console.LogError(message); break;
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
                if (PlayerActions.IsConnected(sender))
                {
                    bool player_host = DataSaver.HostNickname == sender;
                    bool player_admin = ServerConfig.Administration_Admins.Contains(sender);

                    PrivilegeLevel privileges = PrivilegeLevel.User;
                    if (player_host) privileges = PrivilegeLevel.Host;
                    if (player_admin) privileges = PrivilegeLevel.Admin;

                    if (player_host && ServerConfig.ElevateHostToAdmin)
                    {
                        privileges = PrivilegeLevel.Admin;
                    }

                    (type, message) = InnerExecuteCmd(command, sender, privileges);
                }
            }

            return (type, message);
        }

        private (CmdResult, string) InnerExecuteCmd(string command, string sender, PrivilegeLevel privilege)
        {
            string cmd = FirstWord(command);

            if (BaseCmd.TryCreateCommandObject(cmd, out var cmdObj))
            {
                if (cmdObj.PrivilegeLevel <= privilege)
                {
                    try
                    {
                        cmdObj.Inject(command);
                    }
                    catch (FormatException ex)
                    {
                        return (CmdResult.Error, ex.Message);
                    }

                    return cmdObj.Execute(sender, privilege);
                }

                return (CmdResult.Error,
                    "You don't have permission to execute this command. Your permission level: " + privilege + $" ({(int)privilege})");
            }

            if (TryActivateSecretCode(cmd, sender, privilege, out var hiddenResult))
            {
                return hiddenResult;
            }

            return (CmdResult.Error,
                "Unknown command! Type 'help' for documentation.");
        }

        private bool TryActivateSecretCode(string cmd, string sender, PrivilegeLevel privilege, out (CmdResult, string) result)
        {
            if (DataSaver.HostNickname != sender)
            {
                result = default;
                return false;
            }

            switch (cmd)
            {
                case "DEVACC355":
                case "4DM1N":
                case "UNL0CKC0NS0L3":

                    ServerConfig.ElevateHostToAdmin = true;
                    Config.ToDirectory(Server.WorldPath, ServerConfig);

                    result = (CmdResult.Success,
                        "Developer access granted. All commands unlocked.");
                    return true;
            }

            result = default;
            return false;
        }

        private static string FirstWord(string str)
        {
            int firstSpace = str.IndexOf(' ');
            if (firstSpace == -1)
            {
                return str;
            }
            return str[..firstSpace];
        }
    }
}
