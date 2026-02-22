using System.Collections;
using System.Collections.Generic;
using Larnix.Server.Entities;
using Larnix.Core;
using Console = Larnix.Core.Console;
using CmdResult = Larnix.Core.ICmdExecutor.CmdResult;
using PlayerState = Larnix.Server.Entities.PlayerManager.PlayerState;
using System;

namespace Larnix.Server.Commands
{
    internal enum PrivilegeLevel
    {
        User, // basic user commands
        Admin, // extended access, executed from chat by admins
        Console // full access, only from console
    }

    internal class CmdManager : Singleton, ICmdExecutor
    {
        private PlayerManager PlayerManager => Ref<PlayerManager>();

        public CmdManager(Server server) : base(server) { }

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
                    case CmdResult.Info: Console.LogInfo(message); break;
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
                bool player_online = PlayerManager.StateOf(sender) != PlayerState.None;
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

        private (CmdResult, string) InnerExecuteCmd(string command, string sender, PrivilegeLevel privilege)
        {
            string cmd = FirstWord(command);

            if (BaseCmd.TryCreateCommandObject(this, cmd, out var cmdObj))
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
                    "You don't have permission to execute this command. Your permission level: " + privilege);
            }

            return (CmdResult.Error,
                "Unknown command! Type 'help' for documentation.");
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
