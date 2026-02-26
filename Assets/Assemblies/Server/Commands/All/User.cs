using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Larnix.Core;
using Larnix.Server.Entities;
using Larnix.Socket.Backend;
using CmdResult = Larnix.Core.ICmdExecutor.CmdResult;

namespace Larnix.Server.Commands.All
{
    internal class User : BaseCmd
    {
        public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Host;
        public override string Pattern => $"{Name} <params...>";
        public override string Hint => $"Type 'help {Name}' for more information.";
        public override string ShortDescription => "Manages user accounts.";
        
        public override string LongDescription => ShortDescription + " Usage:\n" +
            "user add <username> <password> - Registers a new user.\n" +
            "user rename <oldusername> <newusername> - Renames a user.\n" +
            "user changepass <username> <newpassword> - Changes a user's password.\n" +
            "user delete <username> - Deletes a user.\n" +
            "user resetlimits - Resets all hashing and registration user limits.\n" +
            "user list - Lists all registered users.\n" +
            "user deleteall - Deletes all users.";

        private QuickServer QuickServer => GlobRef.Get<QuickServer>();
        private PlayerActions PlayerActions => GlobRef.Get<PlayerActions>();
        private IUserManager UserManager => GlobRef.Get<IUserManager>();

        private string _subname;
        private string _username;
        private string _password;
        private string _oldusername;
        private string _newusername;
        private string _newpassword;

        public override void Inject(string command)
        {
            if (!TrySplitMin(command, 2, out string[] parts, out int length))
                throw FormatException(InvalidCmdFormat);

            string subname = parts[1];
            string subcommand = string.Join(' ', parts[1..]);
            _subname = subname;

            var commands = new Dictionary<string, Action<string[]>>
            {
                ["add"] = args =>
                {
                    if (!Parsing.TryParseNickname(args[1], out string username)) throw FormatException("Invalid username.");
                    if (!Parsing.TryParsePassword(args[2], out string password)) throw FormatException("Invalid password.");
                    _username = username;
                    _password = password;
                },
                ["rename"] = args =>
                {
                    if (!Parsing.TryParseNickname(args[1], out string oldusername)) throw FormatException("Invalid username.");
                    if (!Parsing.TryParseNickname(args[2], out string newusername)) throw FormatException("Invalid username.");
                    _oldusername = oldusername;
                    _newusername = newusername;
                },
                ["changepass"] = args =>
                {
                    if (!Parsing.TryParseNickname(args[1], out string username)) throw FormatException("Invalid username.");
                    if (!Parsing.TryParsePassword(args[2], out string password)) throw FormatException("Invalid password.");
                    _username = username;
                    _newpassword = password;
                },
                ["delete"] = args =>
                {
                    if (!Parsing.TryParseNickname(args[1], out string username)) throw FormatException("Invalid username.");
                    _username = username;
                },
                ["resetlimits"] = args => { },
                ["list"] = args => { },
                ["deleteall"] = args => { }
            };

            if (!commands.TryGetValue(subname, out var action))
                throw FormatException(InvalidCmdFormat);

            int expectedArgs = subname switch
            {
                "add" or "rename" or "changepass" => 3,
                "delete" => 2,
                "resetlimits" or "list" or "deleteall" => 1,
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
                ["rename"] = ExecuteRename,
                ["changepass"] = ExecuteChangePass,
                ["delete"] = ExecuteDelete,
                ["resetlimits"] = ExecuteResetLimits,
                ["list"] = ExecuteList,
                ["deleteall"] = ExecuteDeleteAll
            };

            if (executes.TryGetValue(_subname, out var execute))
                return execute();
            
            return (CmdResult.Error, "Invalid subcommand.");
        }

        private (CmdResult, string) ExecuteAdd()
        {
            if (UserManager.TryAddUserSync(_username, _password))
            {
                return (CmdResult.Success,
                    $"User '{_username}' added successfully.");
            }

            return (CmdResult.Error,
                $"Failed to add user '{_username}'.");
        }

        private (CmdResult, string) ExecuteRename()
        {
            if (UserManager.TryRenameUser(_oldusername, _newusername))
            {
                return (CmdResult.Success,
                    $"User '{_oldusername}' renamed to '{_newusername}' successfully.");
            }

            return (CmdResult.Error,
                $"Failed to rename user '{_oldusername}' to '{_newusername}'.");
        }

        private (CmdResult, string) ExecuteChangePass()
        {
            if (UserManager.TryChangePasswordSync(_username, _newpassword))
            {
                return (CmdResult.Success,
                    $"Password for user '{_username}' changed successfully.");
            }

            return (CmdResult.Error,
                $"Failed to change password for user '{_username}'.");
        }

        private (CmdResult, string) ExecuteDelete()
        {
            if (UserManager.TryDeleteUserLink(_username))
            {
                return (CmdResult.Success,
                    $"User '{_username}' deleted successfully.");
            }

            return (CmdResult.Error,
                $"Failed to delete user '{_username}'.");
        }

        private (CmdResult, string) ExecuteResetLimits()
        {
            UserManager.ResetLimits();
            return (CmdResult.Success, "User limits have been reset.");
        }

        private (CmdResult, string) ExecuteList()
        {
            IPEndPoint EndPointOf(string nick)
            {
                if (QuickServer.TryGetClientEndPoint(nick, out var endPoint))
                    return endPoint;
                
                return null;
            }

            string StateOf(string nick)
            {
                return PlayerActions
                    .StateOf(nick)
                    .ToString()
                    .ToUpperInvariant();
            }

            string NONE = PlayerActions.PlayerState.None
                .ToString().ToUpperInvariant();

            IEnumerable<string> lines = UserManager.AllUsernames()
                .OrderBy(nick => StateOf(nick) == NONE ? 1 : 0)
                .ThenBy(nick => nick)
                .Select(nick =>
                {
                    IPEndPoint endPoint = EndPointOf(nick);
                    string state = StateOf(nick);

                    return state != NONE ?
                        $"{nick} from {endPoint} is {state}." :
                        $"{nick} is OFFLINE.";
                });

            return (CmdResult.Raw,
                MakeRobustList("USER LIST", lines));
        }

        private (CmdResult, string) ExecuteDeleteAll()
        {
            string[] allUsers = UserManager.AllUsernames().ToArray();
            if (allUsers.Any(nick => UserManager.IsOnline(nick)))
            {
                return (CmdResult.Error,
                    "Cannot delete all users while some are still online. Disconnect all users and try again.");
            }

            List<string> failed = new();
            foreach (string nick in allUsers)
            {
                if (!UserManager.TryDeleteUserLink(nick))
                {
                    failed.Add(nick);
                }
            }

            if (!failed.Any())
            {
                return (CmdResult.Success,
                    "All users deleted successfully.");
            }
            else
            {
                string listedFailed = string.Join(", ", failed.Select(nick => $"'{nick}'"));
                return (CmdResult.Error,
                    $"Failed to delete following users: {listedFailed}.");
            }
        }
    }
}
